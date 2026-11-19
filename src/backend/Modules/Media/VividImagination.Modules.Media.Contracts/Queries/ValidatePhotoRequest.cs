namespace VividImagination.Modules.Media.Contracts.Queries;

public record ValidatePhotoRequest(Guid PhotoId);

public record PhotoValidationResponse(
    Guid PhotoId,
    bool Exists,
    bool IsDeleted = false,
    string? OwnerId = null,
    string? Name = null,
    string? Description = null);
