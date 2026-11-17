using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public sealed record GetFeedQuery(int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<FeedItemDto>>, ICacheRequest
{
    public CacheKey CacheKey => FeedCacheDefaults.FeedPageCacheKey;
    public object[] CacheKeyParameters => [Page, PageSize];
}
