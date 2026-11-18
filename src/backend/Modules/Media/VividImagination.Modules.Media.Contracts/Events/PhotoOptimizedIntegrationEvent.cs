using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Media.Contracts.Events;

public record PhotoOptimizedIntegrationEvent(
    Guid PhotoId,
    string BlurHash,
    string OriginalKey,
    string MediumKey,
    string ThumbnailKey,
    int Width,
    int Height,
    long TotalSize,
    DateTime OptimizedAt) : IntegrationEvent(PhotoId, OptimizedAt);
