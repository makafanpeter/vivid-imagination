using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Feed.Contracts.Events;

public record FeedItemViewedIntegrationEvent(
    Guid PhotoId,
    Guid UserId,
    DateTime ViewedAt) : IntegrationEvent(PhotoId, ViewedAt);
