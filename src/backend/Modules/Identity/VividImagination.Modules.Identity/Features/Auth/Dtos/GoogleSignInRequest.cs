using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Auth.Dtos;

public sealed record GoogleSignInRequest(
    string IdToken,
    SafeBrowsingMode? SafeBrowsingMode = null);

public sealed record GoogleSignInResult(
    long UserId,
    string Email,
    string? FullName,
    SafeBrowsingMode SafeBrowsingMode,
    bool IsNewUser);
