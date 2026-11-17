using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VividImagination.Modules.Identity.Features.Auth.Services;

public class GoogleTokenValidator(
    IConfiguration configuration,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    public Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(idToken))
            {
                logger.LogWarning("Provided token is not a readable JWT token.");
                return Task.FromResult<GoogleTokenPayload?>(null);
            }

            var jwt = handler.ReadJwtToken(idToken);

            var subject = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var emailVerifiedStr = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
            var givenName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
            var familyName = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
            {
                logger.LogWarning("Token is missing subject or email claims.");
                return Task.FromResult<GoogleTokenPayload?>(null);
            }

            var emailVerified = bool.TryParse(emailVerifiedStr, out var ev) && ev;

            var payload = new GoogleTokenPayload(
                subject,
                email,
                emailVerified,
                givenName,
                familyName);

            return Task.FromResult<GoogleTokenPayload?>(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse/validate Google ID token.");
            return Task.FromResult<GoogleTokenPayload?>(null);
        }
    }
}
