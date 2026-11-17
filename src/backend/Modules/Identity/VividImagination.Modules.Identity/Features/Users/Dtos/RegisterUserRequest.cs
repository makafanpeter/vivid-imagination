using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Users.Dtos;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    SafeBrowsingMode? SafeBrowsingMode = null);

public sealed record RegisterUserResult(
    long UserId,
    string Email,
    string? FullName,
    SafeBrowsingMode SafeBrowsingMode,
    DateTimeOffset CreatedOn);
