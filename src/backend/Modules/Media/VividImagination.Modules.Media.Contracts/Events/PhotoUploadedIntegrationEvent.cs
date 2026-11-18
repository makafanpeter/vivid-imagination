using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Media.Contracts.Events;

public record PhotoUploadedIntegrationEvent(
    Guid PhotoId,
    Guid UserId,
    string StorageKey,
    string BlurHash,
    string Caption,
    DateTime UploadedAt) : IntegrationEvent(PhotoId, UploadedAt);
