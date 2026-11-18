namespace VividImagination.Modules.Identity.Features.Auth.Services;

public record GoogleTokenPayload(
    string Subject,
    string Email,
    bool EmailVerified,
    string? GivenName,
    string? FamilyName);

public interface IGoogleTokenValidator
{
    Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken ct = default);
}
