using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Modules.Feed.Features.Feeds.Caching;

public static class FeedCacheDefaults
{
    public const string FeedPrefix = "vivid:feed";
    public const string PersonalizedFeedPrefix = "vivid:feed:personalized";
    public const string UserProfilePrefix = "vivid:feed:profile";

    public static readonly CacheKey FeedPageCacheKey = new(
        "vivid:feed:page:{0}:size:{1}",
        FeedPrefix)
    {
        CacheTime = 2
    };

    public static readonly CacheKey PersonalizedFeedCacheKey = new(
        "vivid:feed:personalized:{0}:page:{1}:size:{2}",
        PersonalizedFeedPrefix, FeedPrefix)
    {
        CacheTime = 1
    };

    public static readonly CacheKey UserProfileCacheKey = new(
        "vivid:feed:profile:{0}",
        UserProfilePrefix, FeedPrefix)
    {
        CacheTime = 5
    };
}
