using System.Net;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using StackExchange.Redis;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Caching.Redis
{
    /// <summary>
    /// Owns the single, long-lived <see cref="ConnectionMultiplexer"/> for the process.
    /// Register as a SINGLETON. The multiplexer reconnects on its own, so there is no manual reconnect logic.
    /// </summary>
    public sealed class RedisConnectionWrapper : IRedisConnectionWrapper
    {
        private const int ScanPageSize = 500;

        private readonly AsyncLazy<ConnectionMultiplexer> _connection;

        public RedisConnectionWrapper(IOptions<ApplicationSettings> config)
        {
            var connectionString = config.Value.Cache.ConnectionString;

            _connection = new AsyncLazy<ConnectionMultiplexer>(async () =>
            {
                var options = ConfigurationOptions.Parse(connectionString);

                // Don't crash the app at startup if Redis is down; keep retrying in the background.
                options.AbortOnConnectFail = false;

                // Fail fast so a Redis outage degrades to "cache miss" rather than hanging requests.
                options.ConnectTimeout = Math.Max(options.ConnectTimeout, 5000);
                options.AsyncTimeout = Math.Min(options.AsyncTimeout, 2000);

                return await ConnectionMultiplexer.ConnectAsync(options);
            });
        }

        #region Methods

        public async Task<IDatabase> GetDatabaseAsync(int db)
        {
            var connection = await _connection;
            return connection.GetDatabase(db);
        }

        public async Task<ISubscriber> GetSubscriberAsync()
        {
            var connection = await _connection;
            return connection.GetSubscriber();
        }

        public async Task<IServer?> GetServerAsync(EndPoint endPoint)
        {
            var connection = await _connection;
            return connection.GetServer(endPoint);
        }

        public async Task<EndPoint[]?> GetEndPointsAsync()
        {
            var connection = await _connection;
            return connection.GetEndPoints();
        }

        public async Task FlushDatabaseAsync(RedisDatabaseNumber db)
        {
            var endPoints = await GetEndPointsAsync();
            if (endPoints == null) return;

            foreach (var endPoint in endPoints)
            {
                var server = await GetServerAsync(endPoint);
                if (server is null || !server.IsConnected || server.IsReplica) continue;

                await server.FlushDatabaseAsync((int)db);
            }
        }

        public async Task ClearByPatternAsync(RedisDatabaseNumber db, string pattern)
        {
            var endPoints = await GetEndPointsAsync();
            if (endPoints == null) return;

            var database = await GetDatabaseAsync((int)db);

            foreach (var endPoint in endPoints)
            {
                var server = await GetServerAsync(endPoint);

                // Replicas are read-only and would produce duplicate scans.
                if (server is null || !server.IsConnected || server.IsReplica) continue;

                var batchKeys = new List<RedisKey>(ScanPageSize);

                // Pass the database explicitly: without it SCAN runs against the default DB, not `db`.
                await foreach (var key in server.KeysAsync((int)db, $"{pattern}*", ScanPageSize))
                {
                    batchKeys.Add(key);

                    if (batchKeys.Count >= ScanPageSize)
                    {
                        await DeleteAsync(database, batchKeys);
                        batchKeys.Clear();
                    }
                }

                if (batchKeys.Count > 0)
                    await DeleteAsync(database, batchKeys);
            }
        }

        public void Dispose()
        {
            if (_connection.Task.IsCompletedSuccessfully)
                _connection.Task.Result.Dispose();
        }

        #endregion

        // Per-key DELs pipelined in one batch: cluster-safe (no CROSSSLOT) and a single round trip.
        private static async Task DeleteAsync(IDatabase database, List<RedisKey> keys)
        {
            var batch = database.CreateBatch();
            var tasks = keys.Select(k => batch.KeyDeleteAsync(k)).ToArray();
            batch.Execute();
            await Task.WhenAll(tasks);
        }
    }
}