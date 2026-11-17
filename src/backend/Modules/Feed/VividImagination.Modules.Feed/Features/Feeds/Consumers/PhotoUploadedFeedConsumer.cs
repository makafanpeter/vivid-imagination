using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoUploadedFeedConsumer : IConsumer<PhotoUploadedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PhotoUploadedFeedConsumer> _logger;

    public PhotoUploadedFeedConsumer(IMongoRepository<FeedItem, Guid> repository, IDistributedCache cache, ILogger<PhotoUploadedFeedConsumer> logger)
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

        await _repository.AddAsync(feedItem, context.CancellationToken);
        await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
    }
}