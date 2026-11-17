using MassTransit;
using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoDeletedFeedConsumer : IConsumer<PhotoDeletedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IMediator _mediator;

    public PhotoDeletedFeedConsumer(IMongoRepository<FeedItem, Guid> repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<PhotoDeletedIntegrationEvent> context)
    {
        await _repository.DeleteByIdAsync(context.Message.PhotoId, context.CancellationToken);
        await _mediator.Send(new InvalidateFeedCacheCommand(), context.CancellationToken);
    }
}
