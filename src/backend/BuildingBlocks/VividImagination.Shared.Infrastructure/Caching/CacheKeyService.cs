using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using VividImagination.Shared.Domain.Entities;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Caching
{
    public abstract partial class CacheKeyService
    {
        #region Constants

        /// <summary>
        /// Gets an algorithm used to create the hash value of identifiers need to cache
        /// </summary>
        private string HashAlgorithm => "SHA1";

        #endregion

        #region Fields

        protected readonly ApplicationSettings AppSettings;
        

        #endregion

        #region Ctor

        protected CacheKeyService(IOptions<ApplicationSettings> appSettings)
        {
            AppSettings = appSettings.Value;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare the cache key prefix
        /// </summary>
        /// <param name="prefix">Cache key prefix</param>
        /// <param name="prefixParameters">Parameters to create cache key prefix</param>
        protected virtual string PrepareKeyPrefix(string prefix, params object[] prefixParameters)
        {
            return prefixParameters?.Any() ?? false
                ? string.Format(CultureInfo.InvariantCulture, prefix,
                    prefixParameters.Select(CreateCacheKeyParameters).ToArray())
                : prefix;
        }

        /// <summary>
        /// Create the hash value of the passed identifiers
        /// </summary>
        /// <param name="ids">Collection of identifiers</param>
        /// <returns>String hash value</returns>
        private string CreateIdsHashCore<T>(IEnumerable<T> ids) where T : IComparable<T>
        {
            var identifiers = ids.ToList();

            if (!identifiers.Any())
                return string.Empty;

            var identifiersString = string.Join(", ", identifiers.OrderBy(id => id));
            return CommonHelper.CreateHash(Encoding.UTF8.GetBytes(identifiersString), HashAlgorithm);
        }
        
        
        /// <summary>
        /// Create the hash value of the passed identifiers
        /// </summary>
        /// <param name="ids">Collection of identifiers</param>
        /// <returns>String hash value</returns>
        protected virtual string CreateIdsHash(IEnumerable<long> ids)
        {
            return CreateIdsHashCore(ids);
        }

        /// <summary>
        /// Create the hash value of the passed identifiers
        /// </summary>
        /// <param name="ids">Collection of identifiers</param>
        /// <returns>String hash value</returns>
        protected virtual string CreateIdsHash(IEnumerable<Guid> ids)
        {
            return CreateIdsHashCore(ids);
        }
        

        /// <summary>
        /// Converts an object to cache parameter
        /// </summary>
        /// <param name="parameter">Object to convert</param>
        /// <returns>Cache parameter</returns>
        protected virtual object CreateCacheKeyParameters(object parameter)
        {
            return parameter switch
            {
                null => "null",
                IEnumerable<long> ids => CreateIdsHash(ids),
                IEnumerable<Guid> ids => CreateIdsHash(ids),
                IEnumerable<IHasKey<long>> entities => CreateIdsHash(entities.Select(e => e.Id)),
                IEnumerable<IHasKey<Guid>> entities => CreateIdsHash(entities.Select(e => e.Id)),
                IHasKey<long> entity => entity.Id,
                IHasKey<Guid> entity => entity.Id,
                decimal param => param.ToString(CultureInfo.InvariantCulture),
                _ => parameter
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a copy of cache key and fills it by passed parameters
        /// </summary>
        /// <param name="cacheKey">Initial cache key</param>
        /// <param name="cacheKeyParameters">Parameters to create cache key</param>
        /// <returns>Cache key</returns>
        public virtual CacheKey PrepareKey(CacheKey cacheKey, params object[] cacheKeyParameters)
        {
            return cacheKey.Create(CreateCacheKeyParameters, cacheKeyParameters);
        }

        /// <summary>
        /// Create a copy of cache key using the default cache time and fills it by passed parameters
        /// </summary>
        /// <param name="cacheKey">Initial cache key</param>
        /// <param name="cacheKeyParameters">Parameters to create cache key</param>
        /// <returns>Cache key</returns>
        public virtual CacheKey PrepareKeyForDefaultCache(CacheKey cacheKey, params object[] cacheKeyParameters)
        {
            var key = cacheKey.Create(CreateCacheKeyParameters, cacheKeyParameters);

            key.CacheTime = AppSettings.Cache.DefaultCacheTime;

            return key;
        }

        /// <summary>
        /// Create a copy of cache key using the short cache time and fills it by passed parameters
        /// </summary>
        /// <param name="cacheKey">Initial cache key</param>
        /// <param name="cacheKeyParameters">Parameters to create cache key</param>
        /// <returns>Cache key</returns>
        public virtual CacheKey PrepareKeyForShortTermCache(CacheKey cacheKey, params object[] cacheKeyParameters)
        {
           
            var key = cacheKey.Create(CreateCacheKeyParameters, cacheKeyParameters);

            key.CacheTime = AppSettings.Cache.ShortTermCacheTime;

            return key;
        }

        #endregion
    }
}