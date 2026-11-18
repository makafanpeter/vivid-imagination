namespace VividImagination.Shared.Infrastructure.Caching.Redis
{
    /// <summary>
    /// Represents redis database number enumeration
    /// </summary>
    public enum RedisDatabaseNumber
    {
        /// <summary>
        /// Database for caching
        /// </summary>
        Cache = 2,
        /// <summary>
        /// Database for plugins
        /// </summary>
        Plugin = 1,
        /// <summary>
        /// Database for data protection keys
        /// </summary>
        DataProtectionKeys = 0
    }
}