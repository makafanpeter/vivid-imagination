using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoDeletedFeedConsumer : IConsumer<PhotoDeletedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IDistributedCache _cache;

    public PhotoDeletedFeedConsumer(IMongoRepository<FeedItem, Guid> repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<PhotoDeletedIntegrationEvent> context)
    {
        await _repository.DeleteByIdAsync(context.Message.PhotoId, context.CancellationToken);
        await _cache.RemoveAsync("feed_page_1", context.CancellationToken);
    }
}