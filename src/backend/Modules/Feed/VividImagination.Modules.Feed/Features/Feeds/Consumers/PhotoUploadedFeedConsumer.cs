using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Social.Contracts.Events;

namespace VividImagination.Modules.Feed.Consumers;

public class PhotoUploadedFeedConsumer : IConsumer<PhotoUploadedIntegrationEvent>
{
    private readonly IFeedRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PhotoUploadedFeedConsumer> _logger;

    public PhotoUploadedFeedConsumer(IFeedRepository repository, IDistributedCache cache, ILogger<PhotoUploadedFeedConsumer> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PhotoUploadedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("FeedModule: Creating FeedItem for PhotoId {PhotoId}", msg.PhotoId);

        var feedItem = new FeedItem
        {
            Id = Guid.NewGuid(),
            PhotoId = msg.PhotoId,
            UserId = msg.UserId,
            StorageKey = msg.StorageKey,
            BlurHash = msg.BlurHash,
            Caption = msg.Caption,
            Upvotes = 0,
            Downvotes = 0,
            Score = FeedRankingCalculator.CalculateScore(0, 0, msg.UploadedAt),
            IsUnsafe = false,
            BlurRequired = false,
            Verdict = "Pending",
            UploadedAt = msg.UploadedAt,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.UpsertFeedItemAsync(feedItem, context.CancellationToken);
        await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
    }
}

public class PhotoModeratedFeedConsumer : IConsumer<PhotoModeratedIntegrationEvent>
{
    private readonly IFeedRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PhotoModeratedFeedConsumer> _logger;

    public PhotoModeratedFeedConsumer(IFeedRepository repository, IDistributedCache cache, ILogger<PhotoModeratedFeedConsumer> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PhotoModeratedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("FeedModule: Updating safety flags for PhotoId {PhotoId}, Verdict: {Verdict}", msg.PhotoId, msg.Verdict);

        if (msg.Verdict == "Rejected")
        {
            // Severe violation: Purge from public feed projections
            await _repository.DeleteByPhotoIdAsync(msg.PhotoId, context.CancellationToken);
        }
        else
        {
            var item = await _repository.GetByPhotoIdAsync(msg.PhotoId, context.CancellationToken);
            if (item != null)
            {
                item.IsUnsafe = msg.IsUnsafe;
                item.BlurRequired = msg.BlurRequired;
                item.SafetyCategory = msg.SafetyCategory;
                item.Verdict = msg.Verdict;
                item.LastUpdatedAt = DateTime.UtcNow;

                await _repository.UpsertFeedItemAsync(item, context.CancellationToken);
            }
        }

        // FR-FEED-03: Invalidate distributed Redis cache
        await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
    }
}

public class PhotoVotedFeedConsumer : IConsumer<PhotoVotedIntegrationEvent>
{
    private readonly IFeedRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PhotoVotedFeedConsumer> _logger;

    public PhotoVotedFeedConsumer(IFeedRepository repository, IDistributedCache cache, ILogger<PhotoVotedFeedConsumer> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PhotoVotedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("FeedModule: Updating votes for PhotoId {PhotoId}, Upvotes: {Up}, Downvotes: {Down}",
            msg.PhotoId, msg.TotalUpvotes, msg.TotalDownvotes);

        var item = await _repository.GetByPhotoIdAsync(msg.PhotoId, context.CancellationToken);
        if (item != null)
        {
            item.Upvotes = msg.TotalUpvotes;
            item.Downvotes = msg.TotalDownvotes;
            item.Score = FeedRankingCalculator.CalculateScore(item.Upvotes, item.Downvotes, item.UploadedAt);
            item.LastUpdatedAt = DateTime.UtcNow;

            await _repository.UpsertFeedItemAsync(item, context.CancellationToken);
            await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
        }
    }
}

public class PhotoDeletedFeedConsumer : IConsumer<PhotoDeletedIntegrationEvent>
{
    private readonly IFeedRepository _repository;
    private readonly IDistributedCache _cache;

    public PhotoDeletedFeedConsumer(IFeedRepository repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<PhotoDeletedIntegrationEvent> context)
    {
        await _repository.DeleteByPhotoIdAsync(context.Message.PhotoId, context.CancellationToken);
        await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
    }
}
