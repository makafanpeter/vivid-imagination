using MassTransit;
using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoDeletedFeedConsumer(IMongoRepository<FeedItem, Guid> repository, IMediator mediator)
    : IConsumer<PhotoDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PhotoDeletedIntegrationEvent> context)
    {
        await repository.DeleteByIdAsync(context.Message.PhotoId, context.CancellationToken);
        await mediator.Send(new InvalidateFeedCacheCommand(), context.CancellationToken);
    }
}
