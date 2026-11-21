using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoModeratedFeedConsumer : IConsumer<PhotoModeratedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<PhotoModeratedFeedConsumer> _logger;

    public PhotoModeratedFeedConsumer(
        IMongoRepository<FeedItem, Guid> repository,
        IMediator mediator,
        ILogger<PhotoModeratedFeedConsumer> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PhotoModeratedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("FeedModule: Updating safety flags for PhotoId {PhotoId}, Verdict: {Verdict}", msg.PhotoId, msg.Verdict);

        if (msg.Verdict == "Rejected")
        {
            // Severe violation: Purge from public feed projections
            await _repository.DeleteByIdAsync(msg.PhotoId, context.CancellationToken);
        }
        else
        {
            var item = await _repository.FindByIdAsync(msg.PhotoId, context.CancellationToken);
            if (item != null)
            {
                item.IsUnsafe = msg.IsUnsafe;
                item.BlurRequired = msg.BlurRequired;
                item.SafetyCategory = msg.SafetyCategory;
                item.Verdict = msg.Verdict;
                item.UpdatedOn = DateTime.UtcNow;

                await _repository.UpdateAsync(item, context.CancellationToken);
            }
        }

        await _mediator.Send(new InvalidateFeedCacheCommand(), context.CancellationToken);
    }
}
