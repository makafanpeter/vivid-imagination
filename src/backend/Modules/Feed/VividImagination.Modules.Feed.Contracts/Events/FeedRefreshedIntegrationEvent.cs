using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Feed.Contracts.Events;

public record FeedRefreshedIntegrationEvent(
    Guid UserId,
    int ItemsCount,
    DateTime RefreshedAt) : IntegrationEvent(UserId, RefreshedAt);
