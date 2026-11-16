using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Media.Contracts.Events;

public record PhotoDeletedIntegrationEvent(
    Guid PhotoId,
    string StorageKey,
    DateTime DeletedAt) : IntegrationEvent(PhotoId, DeletedAt);
