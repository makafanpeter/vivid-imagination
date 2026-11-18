namespace VividImagination.Shared.Infrastructure.Caching;

public interface IInvalidateCacheRequest
{
    CacheKey? CacheKey => null;
    object[] CacheKeyParameters => [];
    string? Prefix => null;
    object[] PrefixParameters => [];
    IEnumerable<string> Prefixes => [];
}
