using MassTransit;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Contracts.Events;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Shared.Infrastructure.Caching;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class FeedInteractionConsumer : IConsumer<FeedInteractionOccurredIntegrationEvent>
{
    private readonly IMongoRepository<UserInterestProfile, Guid> _profileRepository;
    private readonly IMongoRepository<UserInteraction, Guid> _interactionRepository;
    private readonly IMongoRepository<FeedItem, Guid> _feedItemRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly ILogger<FeedInteractionConsumer> _logger;

    public FeedInteractionConsumer(
        IMongoRepository<UserInterestProfile, Guid> profileRepository,
        IMongoRepository<UserInteraction, Guid> interactionRepository,
        IMongoRepository<FeedItem, Guid> feedItemRepository,
        IStaticCacheManager cacheManager,
        ILogger<FeedInteractionConsumer> logger)
    {
        _profileRepository = profileRepository;
        _interactionRepository = interactionRepository;
        _feedItemRepository = feedItemRepository;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FeedInteractionOccurredIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        // 1. Idempotency check: Ignore duplicate EventId
        var existing = await _interactionRepository.FindOneAsync(x => x.EventId == msg.EventId, ct);
        if (existing != null)
        {
            _logger.LogWarning("FeedInteractionConsumer: Duplicate interaction event {EventId} ignored", msg.EventId);
            return;
        }

        // 2. Resolve tags
        IReadOnlyList<string> tags = msg.Tags ?? [];
        if (tags.Count == 0 && msg.PhotoId != Guid.Empty)
        {
            var feedItem = await _feedItemRepository.FindByIdAsync(msg.PhotoId, ct);
            if (feedItem != null)
            {
                tags = TagExtractor.ExtractTags(feedItem.Caption);
            }
        }

        var normalizedTags = tags.Select(t => t.ToLowerInvariant()).Distinct().ToList();

        // 3. Persist audit interaction record
        var interaction = new UserInteraction
        {
            Id = Guid.NewGuid(),
            EventId = msg.EventId,
            UserId = msg.UserId,
            PhotoId = msg.PhotoId,
            CreatorId = msg.CreatorId,
            InteractionType = msg.InteractionType,
            FeedPosition = msg.FeedPosition,
            DwellDurationMs = msg.DwellDurationMs,
            Tags = normalizedTags,
            Timestamp = msg.Timestamp
        };
        await _interactionRepository.AddAsync(interaction, ct);

        // 4. Retrieve or create UserInterestProfile
        var profile = await _profileRepository.FindOneAsync(x => x.UserId == msg.UserId, ct);
        bool isNew = false;
        if (profile == null)
        {
            isNew = true;
            profile = new UserInterestProfile
            {
                Id = Guid.NewGuid(),
                UserId = msg.UserId,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        var creatorKey = msg.CreatorId.ToString();

        // 5. Update affinities and preferences
        switch (msg.InteractionType.ToLowerInvariant())
        {
            case "upvote":
                foreach (var tag in normalizedTags)
                {
                    profile.TagAffinities[tag] = profile.TagAffinities.GetValueOrDefault(tag, 0.0) + 0.5;
                }
                if (msg.CreatorId != Guid.Empty)
                {
                    profile.CreatorAffinities[creatorKey] = profile.CreatorAffinities.GetValueOrDefault(creatorKey, 0.0) + 1.0;
                }
                profile.DownvotedPhotoIds.Remove(msg.PhotoId);
                break;

            case "downvote":
                foreach (var tag in normalizedTags)
                {
                    var newScore = profile.TagAffinities.GetValueOrDefault(tag, 0.0) - 0.5;
                    profile.TagAffinities[tag] = newScore;
                    if (newScore < -1.0 && !profile.NegativeTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        profile.NegativeTags.Add(tag);
                    }
                }
                if (msg.CreatorId != Guid.Empty)
                {
                    profile.CreatorAffinities[creatorKey] = profile.CreatorAffinities.GetValueOrDefault(creatorKey, 0.0) - 1.0;
                }
                if (msg.PhotoId != Guid.Empty && !profile.DownvotedPhotoIds.Contains(msg.PhotoId))
                {
                    profile.DownvotedPhotoIds.Add(msg.PhotoId);
                }
                break;

            case "share":
                foreach (var tag in normalizedTags)
                {
                    profile.TagAffinities[tag] = profile.TagAffinities.GetValueOrDefault(tag, 0.0) + 0.8;
                }
                if (msg.CreatorId != Guid.Empty)
                {
                    profile.CreatorAffinities[creatorKey] = profile.CreatorAffinities.GetValueOrDefault(creatorKey, 0.0) + 1.5;
                }
                break;

            case "dwell":
                if (msg.DwellDurationMs >= 3000)
                {
                    foreach (var tag in normalizedTags)
                    {
                        profile.TagAffinities[tag] = profile.TagAffinities.GetValueOrDefault(tag, 0.0) + 0.2;
                    }
                    if (msg.CreatorId != Guid.Empty)
                    {
                        profile.CreatorAffinities[creatorKey] = profile.CreatorAffinities.GetValueOrDefault(creatorKey, 0.0) + 0.3;
                    }
                }
                break;

            case "hide":
                if (msg.PhotoId != Guid.Empty && !profile.HiddenPhotoIds.Contains(msg.PhotoId))
                {
                    profile.HiddenPhotoIds.Add(msg.PhotoId);
                }
                if (msg.CreatorId != Guid.Empty)
                {
                    if (!profile.NegativeCreators.Contains(msg.CreatorId))
                    {
                        profile.NegativeCreators.Add(msg.CreatorId);
                    }
                    profile.CreatorAffinities[creatorKey] = profile.CreatorAffinities.GetValueOrDefault(creatorKey, 0.0) - 2.0;
                }
                break;

            case "impression":
            case "view":
                if (msg.PhotoId != Guid.Empty)
                {
                    profile.RecentlyViewedPhotoIds.Remove(msg.PhotoId);
                    profile.RecentlyViewedPhotoIds.Insert(0, msg.PhotoId);
                    if (profile.RecentlyViewedPhotoIds.Count > 100)
                    {
                        profile.RecentlyViewedPhotoIds.RemoveAt(profile.RecentlyViewedPhotoIds.Count - 1);
                    }
                }
                break;
        }

        profile.TotalInteractions++;
        profile.LastUpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            await _profileRepository.AddAsync(profile, ct);
        }
        else
        {
            await _profileRepository.UpdateAsync(profile, ct);
        }

        // 6. Invalidate caches
        try
        {
            await _cacheManager.RemoveAsync(FeedCacheDefaults.UserProfileCacheKey, profile.UserId);
            await _cacheManager.RemoveByPrefixAsync(FeedCacheDefaults.PersonalizedFeedPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate feed caches after interaction processing");
        }
    }
}
