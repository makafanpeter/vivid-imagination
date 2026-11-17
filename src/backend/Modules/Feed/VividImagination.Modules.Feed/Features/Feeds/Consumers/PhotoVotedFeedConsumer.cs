using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoVotedFeedConsumer : IConsumer<PhotoVotedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PhotoVotedFeedConsumer> _logger;

    public PhotoVotedFeedConsumer(IMongoRepository<FeedItem, Guid> repository, IDistributedCache cache, ILogger<PhotoVotedFeedConsumer> logger)
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

        var item = await _repository.FindByIdAsync(msg.PhotoId, context.CancellationToken);
        if (item != null)
        {
            item.Upvotes = msg.TotalUpvotes;
            item.Downvotes = msg.TotalDownvotes;
            item.Score = FeedRankingCalculator.CalculateScore(item.Upvotes, item.Downvotes, item.UploadedAt);
            item.LastUpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(item, context.CancellationToken);
            await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
        }
    }
}