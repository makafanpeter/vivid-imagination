using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VividImagination.Shared.Infrastructure.Caching.Redis;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Caching
{
    /// <summary>
    /// Redis-backed cache. Talks to <see cref="IDatabase"/> directly (no IDistributedCache) so it can use
    /// Redis Sets for prefix indexes, SET NX locks and pipelining.
    ///
    /// Key layout:
    ///   {key}            -> JSON payload (or the null marker for negative caching)
    ///   idx:{prefix}     -> Redis SET of cache keys that belong to that prefix (shared by all nodes)
    ///
    /// Behaviour:
    ///  - Prefix invalidation works across nodes and restarts (no static in-process key list).
    ///  - Per-key single-flight: concurrent misses on one node run acquire() once.
    ///  - Fail-open: Redis errors are logged and treated as a cache miss.
    ///  - Bad/old JSON is treated as a miss and the key is evicted.
    ///  - TTL jitter (+-10%) and short-lived negative caching of null results.
    /// </summary>
    public partial class DistributedCacheManager(
        IOptions<ApplicationSettings> appSettings,
        IRedisConnectionWrapper redis,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DistributedCacheManager> logger)
        : CacheKeyService(appSettings), ILocker, IStaticCacheManager
    {
        #region Constants

        private const string IndexPrefix = "idx:";
        private const string LockPrefix = "lock:";
        private const int DeleteChunkSize = 500;
        private static readonly byte[] NullMarker = "__null__"u8.ToArray();
        private static readonly TimeSpan NegativeCacheTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan IndexExpiryBuffer = TimeSpan.FromMinutes(5);

        #endregion

        #region Fields

        private readonly PerRequestCache _perRequestCache = new(httpContextAccessor);

        // Per-key single-flight gates (process-local). Ref-counted so the dictionary never grows unbounded.
        private static readonly Dictionary<string, Gate> Gates = new();

        #endregion

        #region Utilities

        private Task<IDatabase> GetDbAsync() => redis.GetDatabaseAsync((int)RedisDatabaseNumber.Cache);

        private static TimeSpan WithJitter(int minutes)
        {
            var jitter = 1 + (Random.Shared.NextDouble() * 0.2 - 0.1); // 0.9 .. 1.1
            return TimeSpan.FromTicks((long)(TimeSpan.FromMinutes(minutes).Ticks * jitter));
        }

        private static string IndexKey(string prefix) => IndexPrefix + prefix;

        private static bool IsRedisFailure(Exception ex) => ex is RedisException or TimeoutException;

        /// <summary>Read from Redis. Never throws for Redis/serialization problems; returns a miss instead.</summary>
        private async Task<(bool hit, T? item)> TryGetItemAsync<T>(CacheKey key)
        {
            try
            {
                var db = await GetDbAsync();
                var value = await db.StringGetAsync(key.Key);

                if (value.IsNullOrEmpty)
                    return (false, default);

                byte[] bytes = value!;

                if (bytes.AsSpan().SequenceEqual(NullMarker))
                    return (true, default); // negative cache hit

                var item = JsonSerializer.Deserialize<T>(bytes);
                _perRequestCache.Set(key.Key, item);

                return (true, item);
            }
            catch (JsonException ex)
            {
                // Payload shape changed (e.g. after a deploy). Treat as a miss and evict the bad entry.
                logger.LogWarning(ex, "Cache payload for {Key} could not be deserialized; evicting", key.Key);
                await SafeRemoveAsync(key.Key);
                return (false, default);
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                logger.LogWarning(ex, "Redis unavailable while reading {Key}; falling back to source", key.Key);
                return (false, default);
            }
        }

        private async Task SafeRemoveAsync(string key)
        {
            try
            {
                var db = await GetDbAsync();
                await db.KeyDeleteAsync(key);
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                logger.LogWarning(ex, "Redis unavailable while removing {Key}", key);
            }
        }

        /// <summary>Write the value and register the key in each prefix index, in one pipelined batch.</summary>
        private async Task WriteAsync(CacheKey key, byte[] bytes, TimeSpan ttl)
        {
            try
            {
                var db = await GetDbAsync();
                var batch = db.CreateBatch();
                var tasks = new List<Task> { batch.StringSetAsync(key.Key, bytes, ttl) };

                foreach (var prefix in key.Prefixes)
                {
                    var idx = IndexKey(prefix);
                    tasks.Add(batch.SetAddAsync(idx, key.Key));

                    // Index lives a bit longer than its longest-lived member. Requires Redis 7+.
                    // On older servers use KeyExpireAsync(idx, ttl + buffer) without the When argument.
                    tasks.Add(batch.KeyExpireAsync(idx, ttl + IndexExpiryBuffer, ExpireWhen.GreaterThanCurrentExpiry));
                }

                batch.Execute();
                await Task.WhenAll(tasks);
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                logger.LogWarning(ex, "Redis unavailable while writing {Key}; value not cached", key.Key);
            }
        }

        /// <summary>Delete keys with pipelined per-key DELs (cluster-safe).</summary>
        private static async Task DeleteKeysAsync(IDatabase db, IEnumerable<RedisKey> keys)
        {
            foreach (var chunk in Enumerable.Chunk(keys, DeleteChunkSize)) // explicit: LocalChamp.Shared.Domain also defines Chunk
            {
                var batch = db.CreateBatch();
                var tasks = chunk.Select(k => batch.KeyDeleteAsync(k)).ToArray();
                batch.Execute();
                await Task.WhenAll(tasks);
            }
        }

        private static async Task<IDisposable> EnterGateAsync(string key)
        {
            Gate gate;
            lock (Gates)
            {
                if (!Gates.TryGetValue(key, out gate!))
                    Gates[key] = gate = new Gate();
                gate.Refs++;
            }

            await gate.Semaphore.WaitAsync();
            return new GateReleaser(key, gate);
        }

        #endregion

        #region Methods

        public void Dispose()
        {
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
        {
            // Per-request cache avoids repeated Redis round trips within one HTTP request.
            if (_perRequestCache.TryGet<T>(key.Key, out var local))
                return local!;

            if (key.CacheTime <= 0)
                return await acquire();

            var (hit, item) = await TryGetItemAsync<T>(key);
            if (hit)
                return item!;

            // Single-flight: only one caller per key on this node runs acquire().
            using (await EnterGateAsync(key.Key))
            {
                (hit, item) = await TryGetItemAsync<T>(key);
                if (hit)
                    return item!;

                var result = await acquire();

                if (result != null)
                    await SetAsync(key, result);
                else
                    await WriteAsync(key, NullMarker, NegativeCacheTime); // negative caching

                return result;
            }
        }

        public Task<T> GetAsync<T>(CacheKey key, Func<T> acquire)
            => GetAsync(key, () => Task.FromResult(acquire()));

        public async Task RemoveAsync(CacheKey cacheKey, params object[] cacheKeyParameters)
        {
            cacheKey = PrepareKey(cacheKey, cacheKeyParameters);

            _perRequestCache.Remove(cacheKey.Key);
            await SafeRemoveAsync(cacheKey.Key);
        }

        public async Task SetAsync(CacheKey key, object? data)
        {
            if ((key?.CacheTime ?? 0) <= 0 || data == null)
                return;

            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, data.GetType());
            _perRequestCache.Set(key.Key, data);

            await WriteAsync(key, bytes, WithJitter(key.CacheTime));
        }

        public async Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters)
        {
            prefix = PrepareKeyPrefix(prefix, prefixParameters);
            _perRequestCache.RemoveByPrefix(prefix);

            try
            {
                var db = await GetDbAsync();
                var idx = IndexKey(prefix);

                var members = await db.SetMembersAsync(idx);
                var keys = members.Select(m => (RedisKey)(string)m!).Append(idx);

                await DeleteKeysAsync(db, keys);
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                // Invalidation failing is serious (stale data), so log at error level.
                logger.LogError(ex, "Redis unavailable while removing by prefix {Prefix}; entries may be stale until TTL", prefix);
            }
        }

        public async Task ClearAsync()
        {
            // HttpContext.Items holds server data we must not wipe, so only the cache entries are removed
            // from the per-request cache, by prefix.
            _perRequestCache.RemoveByPrefix(AppConstants.AppName);

            try
            {
                await redis.ClearByPatternAsync(RedisDatabaseNumber.Cache, AppConstants.AppName);
                await redis.ClearByPatternAsync(RedisDatabaseNumber.Cache, IndexPrefix + AppConstants.AppName);
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                logger.LogError(ex, "Redis unavailable while clearing the cache");
            }
        }

        [Obsolete("Use PerformActionWithLockAsync")]
        public bool PerformActionWithLock(string resource, TimeSpan expirationTime, Action action)
            => PerformActionWithLockAsync(resource, expirationTime, () =>
            {
                action();
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();

        /// <summary>
        /// Distributed lock: SET key token NX PX expiry, released only if we still own it (compare-and-delete).
        /// For correctness-critical locking across Redis failover, use RedLock.net instead.
        /// </summary>
        public async Task<bool> PerformActionWithLockAsync(string resource, TimeSpan expirationTime, Func<Task> action)
        {
            var db = await GetDbAsync();
            var lockKey = LockPrefix + resource;
            var token = Guid.NewGuid().ToString("N");

            if (!await db.LockTakeAsync(lockKey, token, expirationTime))
                return false;

            try
            {
                await action();
                return true;
            }
            finally
            {
                // Only deletes the key if the token still matches, so we never release someone else's lock.
                await db.LockReleaseAsync(lockKey, token);
            }
        }

        #endregion

        #region Nested classes

        private sealed class Gate
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public int Refs;
        }

        private sealed class GateReleaser(string key, Gate gate) : IDisposable
        {
            public void Dispose()
            {
                gate.Semaphore.Release();

                lock (Gates)
                {
                    if (--gate.Refs == 0)
                        Gates.Remove(key);
                }
            }
        }

        /// <summary>
        /// Short-term cache stored in HttpContext.Items for the current request.
        /// Type-safe (a key read as a different type is a miss, not an InvalidCastException)
        /// and uses ordinal prefix matching instead of an unescaped regex.
        /// </summary>
        protected class PerRequestCache(IHttpContextAccessor httpContextAccessor)
        {
            private readonly object _sync = new();

            private IDictionary<object, object?>? GetItems() => httpContextAccessor.HttpContext?.Items;

            public virtual bool TryGet<T>(string key, out T? value)
            {
                lock (_sync)
                {
                    var items = GetItems();
                    if (items != null && items.TryGetValue(key, out var raw) && raw is T typed)
                    {
                        value = typed;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            public virtual void Set(string key, object? data)
            {
                if (data == null) return;

                lock (_sync)
                {
                    var items = GetItems();
                    if (items != null)
                        items[key] = data;
                }
            }

            public virtual void Remove(string key)
            {
                lock (_sync)
                    GetItems()?.Remove(key);
            }

            public virtual void RemoveByPrefix(string prefix)
            {
                lock (_sync)
                {
                    var items = GetItems();
                    if (items == null) return;

                    var matches = items.Keys
                        .OfType<string>()
                        .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var k in matches)
                        items.Remove(k);
                }
            }
        }

        #endregion
    }
}