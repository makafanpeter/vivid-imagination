using System.Net;
using StackExchange.Redis;

namespace VividImagination.Shared.Infrastructure.Caching.Redis
{
    /// <summary>
    /// Represents Redis connection wrapper. Must be registered as a singleton.
    /// </summary>
    public interface IRedisConnectionWrapper : IDisposable
    {
        /// <summary>Obtain an interactive connection to a database inside Redis</summary>
        Task<IDatabase> GetDatabaseAsync(int db);

        /// <summary>Obtain a subscriber for Pub/Sub (used for cross-node invalidation)</summary>
        Task<ISubscriber> GetSubscriberAsync();

        /// <summary>Obtain a configuration API for an individual server</summary>
        Task<IServer?> GetServerAsync(EndPoint endPoint);

        /// <summary>Gets all endpoints defined on the server</summary>
        Task<EndPoint[]?> GetEndPointsAsync();

        /// <summary>Delete all the keys of the database (primary nodes only)</summary>
        Task FlushDatabaseAsync(RedisDatabaseNumber db);

        /// <summary>Delete all keys in the given database that start with <paramref name="pattern"/></summary>
        Task ClearByPatternAsync(RedisDatabaseNumber db, string pattern);
    }
}