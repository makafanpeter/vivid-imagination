using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Consumers;

public class PhotoUploadedFeedConsumer : IConsumer<PhotoUploadedIntegrationEvent>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<PhotoUploadedFeedConsumer> _logger;

    public PhotoUploadedFeedConsumer(
        IMongoRepository<FeedItem, Guid> repository,
        IMediator mediator,
        ILogger<PhotoUploadedFeedConsumer> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PhotoUploadedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("FeedModule: Creating FeedItem for PhotoId {PhotoId}", msg.PhotoId);

        var feedItem = new FeedItem
        {
            Id = msg.PhotoId,
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
            CreatedOn = msg.UploadedAt,
            UpdatedOn = DateTime.UtcNow
        };

        await _repository.AddAsync(feedItem, context.CancellationToken);
        await _mediator.Send(new InvalidateFeedCacheCommand(), context.CancellationToken);
    }
}
