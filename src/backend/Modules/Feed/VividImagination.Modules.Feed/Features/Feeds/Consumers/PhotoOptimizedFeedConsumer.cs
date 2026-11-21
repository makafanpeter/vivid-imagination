using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoOptimizedFeedConsumer(
    IMongoRepository<FeedItem, Guid> repository,
    IMediator mediator,
    ILogger<PhotoOptimizedFeedConsumer> logger)
    : IConsumer<PhotoOptimizedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PhotoOptimizedIntegrationEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("FeedModule: Updating optimized variants and BlurHash for PhotoId {PhotoId}", msg.PhotoId);

        var item = await repository.FindByIdAsync(msg.PhotoId, context.CancellationToken);
        if (item != null)
        {
            item.BlurHash = msg.BlurHash;
            item.StorageKey = msg.MediumKey;
            item.UpdatedOn = msg.OptimizedAt;

            await repository.UpdateAsync(item, context.CancellationToken);
            logger.LogInformation("FeedModule: FeedItem {PhotoId} updated with BlurHash {BlurHash} and StorageKey {StorageKey}", msg.PhotoId, msg.BlurHash, msg.MediumKey);
        }
        else
        {
            logger.LogWarning("FeedModule: FeedItem {PhotoId} not found when applying PhotoOptimizedIntegrationEvent", msg.PhotoId);
        }

        await mediator.Send(new InvalidateFeedCacheCommand(), context.CancellationToken);
    }
}
