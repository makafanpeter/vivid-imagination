using MassTransit;
using MediatR;
using VividImagination.Modules.Feed.Contracts.Events;

namespace VividImagination.Modules.Feed.Features.Feeds.Commands;

public sealed record RecordInteractionRequest(
    Guid PhotoId,
    Guid CreatorId,
    string InteractionType,
    int FeedPosition = 0,
    int DwellDurationMs = 0,
    IReadOnlyList<string>? Tags = null);

public sealed record RecordFeedInteractionCommand(
    Guid UserId,
    Guid PhotoId,
    Guid CreatorId,
    string InteractionType,
    int FeedPosition = 0,
    int DwellDurationMs = 0,
    IReadOnlyList<string>? Tags = null) : IRequest<Unit>;

public class RecordFeedInteractionCommandHandler : IRequestHandler<RecordFeedInteractionCommand, Unit>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RecordFeedInteractionCommandHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Unit> Handle(RecordFeedInteractionCommand request, CancellationToken cancellationToken)
    {
        var evt = new FeedInteractionOccurredIntegrationEvent(
            Guid.NewGuid(),
            request.UserId,
            request.PhotoId,
            request.CreatorId,
            request.InteractionType,
            request.FeedPosition,
            request.DwellDurationMs,
            request.Tags ?? [],
            DateTime.UtcNow);

        await _publishEndpoint.Publish(evt, cancellationToken);
        return Unit.Value;
    }
}
