using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Feed.Contracts.Events;

public record FeedInteractionOccurredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid PhotoId,
    Guid CreatorId,
    string InteractionType, // "Impression", "View", "Dwell", "Upvote", "Downvote", "Hide", "Report", "Share"
    int FeedPosition,
    int DwellDurationMs,
    IReadOnlyList<string> Tags,
    DateTime Timestamp) : IntegrationEvent(EventId, Timestamp);
