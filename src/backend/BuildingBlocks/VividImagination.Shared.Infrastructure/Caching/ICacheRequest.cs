namespace VividImagination.Shared.Infrastructure.Caching;

public interface ICacheRequest
{
    CacheKey CacheKey { get;}
    object[] CacheKeyParameters => [];
}
