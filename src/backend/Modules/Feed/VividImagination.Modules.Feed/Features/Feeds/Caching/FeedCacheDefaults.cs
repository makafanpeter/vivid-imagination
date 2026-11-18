using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Modules.Feed.Features.Feeds.Caching;

public static class FeedCacheDefaults
{
    public const string FeedPrefix = "vivid:feed";

    public static readonly CacheKey FeedPageCacheKey = new(
        "vivid:feed:page:{0}:size:{1}",
        FeedPrefix)
    {
        CacheTime = 2
    };
}
