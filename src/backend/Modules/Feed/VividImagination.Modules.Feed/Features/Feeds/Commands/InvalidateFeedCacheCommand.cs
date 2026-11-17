using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Modules.Feed.Features.Feeds.Commands;

public sealed record InvalidateFeedCacheCommand : IRequest<Unit>, IInvalidateCacheRequest
{
    public CacheKey? CacheKey => null;
    public object[] CacheKeyParameters => [];
    public string? Prefix => FeedCacheDefaults.FeedPrefix;
    public object[] PrefixParameters => [];
    public IEnumerable<string> Prefixes => [FeedCacheDefaults.FeedPrefix];
}
