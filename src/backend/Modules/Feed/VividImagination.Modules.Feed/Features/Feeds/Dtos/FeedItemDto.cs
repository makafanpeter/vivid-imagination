namespace VividImagination.Modules.Feed.Features.Feeds.Dtos;

public sealed record FeedItemDto(
    Guid Id,
    Guid PhotoId,
    Guid UserId,
    string StorageKey,
    string BlurHash,
    string Caption,
    int Upvotes,
    int Downvotes,
    double Score,
    bool IsUnsafe,
    bool BlurRequired,
    string? SafetyCategory,
    string? SensitiveContentWarning,
    DateTimeOffset UploadedAt);
