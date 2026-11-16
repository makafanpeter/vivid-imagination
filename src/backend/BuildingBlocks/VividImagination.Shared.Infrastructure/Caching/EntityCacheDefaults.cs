using VividImagination.Shared.Domain.Entities;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Shared.Infrastructure.Caching;

public static partial class EntityCacheDefaults<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    public static string EntityTypeName => typeof(TEntity).Name.ToLowerInvariant();
    public static string Prefix => $"{AppConstants.AppName}.{EntityTypeName}.";
    public static string ByIdPrefix => $"{Prefix}byid.";
    public static string ByIdsPrefix => $"{Prefix}byids.";
    public static string AllPrefix => $"{Prefix}all.";
    public static string DashboardPrefix => $"{Prefix}dashboard.";

    public static CacheKey ByIdCacheKey => new($"{ByIdPrefix}{{0}}", ByIdPrefix, Prefix);
    public static CacheKey ByIdsCacheKey => new($"{ByIdsPrefix}{{0}}", ByIdsPrefix, Prefix);
    public static CacheKey AllCacheKey => new($"{AllPrefix}", AllPrefix, Prefix);
}