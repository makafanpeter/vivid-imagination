using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VividImagination.Shared.Infrastructure.Web.Helpers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;

namespace VividImagination.Modules.Identity.Controllers.v1;

[ApiController]
[Route("connect")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AuthorizationController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IWebHelper webHelper,
    ILogger<AuthorizationController> logger)
    : ControllerBase
{
    private const string GenericAuthError = "The username or password is incorrect.";
    private const string InvalidTokenError = "The token is no longer valid.";

    private const string ServiceUnavailableError =
        "The authentication service is temporarily unavailable. Please try again.";

    [HttpPost("token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrantAsync(request);
        }


        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo"), HttpPost("userinfo")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                }));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                }));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = user.Id.ToString(),
            [OpenIddictConstants.Claims.Email] = user.Email ?? string.Empty,
            [OpenIddictConstants.Claims.Name] = $"{user.FirstName} {user.LastName}".Trim(),
            [OpenIddictConstants.Claims.PreferredUsername] = user.UserName ?? string.Empty,
            ["safe_browsing_mode"] = user.SafeBrowsingMode.ToString()
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Roles))
        {
            claims[OpenIddictConstants.Claims.Role] = await userManager.GetRolesAsync(user);
        }

        return Ok(claims);
    }

    [HttpGet("logout"), HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }


    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, GenericAuthError);
            }

            // Find user by username or email
            var user = await userManager.FindByNameAsync(request.Username)
                       ?? await userManager.FindByEmailAsync(request.Username);
            if (user == null)
            {
                // Still check password to prevent timing attacks
                await Task.Delay(Random.Shared.Next(100, 300));

                logger.LogWarning(
                    "Failed login attempt for non-existent user. Username: {Username}",
                    request.Username);

                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, GenericAuthError);
            }

            // Validate password
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                logger.LogWarning(
                    "Account locked out for user {UserId}",
                    user.Id);

                return ForbidWithError(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Account is locked. Please try again later or contact support.");
            }


            if (result.IsNotAllowed)
            {
                logger.LogWarning(
                    "Sign-in not allowed for user {UserId}",
                    user.Id);

                return ForbidWithError(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Account is not allowed to sign in. Please verify your email or contact support.");
            }

            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Failed password validation for user {UserId}",
                    user.Id);

                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, GenericAuthError);
            }


            // Create claims principal
            var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());
            if (principal.Identity?.IsAuthenticated != true)
            {
                logger.LogError(
                    "Failed to create authentication principal for user {UserId}",
                    user.Id);

                return ForbidWithError(
                    OpenIddictConstants.Errors.ServerError,
                    "An error occurred during authentication.");
            }

            // Audit successful login
            logger.LogInformation(
                "Successful password grant for user {UserId}. IP: {IpAddress}",
                user.Id,
                webHelper.GetCurrentIpAddress());

            // Return token
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception ex) when (IsRecoverableIdentityError(ex))
        {
            logger.LogWarning(
                ex,
                "Recoverable error during password grant. Username provided: {HasUsername}",
                !string.IsNullOrWhiteSpace(request.Username));

            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, ServiceUnavailableError);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Critical error in password grant. Username provided: {HasUsername}",
                !string.IsNullOrWhiteSpace(request.Username));
            throw;
        }
    }


    private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request)
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                logger.LogWarning("Refresh token authentication failed - invalid token");
                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, InvalidTokenError);
            }

            var userId = authenticateResult.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Refresh token missing subject claim");
                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, InvalidTokenError);
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning(
                    "Refresh token references non-existent user {UserId}",
                    userId);

                return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, InvalidTokenError);
            }
            // Validate security stamp against the already-loaded user without re-querying AspNetUsers
            var expectedStamp = authenticateResult.Principal.GetClaim(signInManager.Options.ClaimsIdentity.SecurityStampClaimType);
            var isStampValid = !userManager.SupportsUserSecurityStamp
                               || string.IsNullOrEmpty(expectedStamp)
                               || await signInManager.ValidateSecurityStampAsync(user, expectedStamp);
            if (!isStampValid)
            {
                logger.LogWarning(
                    "Security stamp validation failed for user {UserId}. Token may have been revoked",
                    user.Id);

                return ForbidWithError(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The token has been revoked. Please sign in again.");
            }

            // Check if user can still sign in
            if (!await signInManager.CanSignInAsync(user))
            {
                logger.LogWarning(
                    "User {UserId} is no longer allowed to sign in",
                    user.Id);

                return ForbidWithError(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The user is no longer allowed to sign in.");
            }

            var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

            // Audit successful refresh
            logger.LogInformation(
                "Successful token refresh for user {UserId}",
                user.Id);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception ex) when (IsRecoverableIdentityError(ex))
        {
            logger.LogWarning(ex, "Recoverable error during refresh token grant");
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, ServiceUnavailableError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error in refresh token grant");
            throw;
        }
    }



    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(
        User user,
        ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Add basic claims (equivalent to AlwaysIncludeUserClaimsInIdToken = true)
        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString())
            .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
            .SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}")
            .SetClaim(OpenIddictConstants.Claims.PreferredUsername, user.UserName)
            .SetClaim("safe_browsing_mode", user.SafeBrowsingMode.ToString());
        

        

        // Add user claims (custom claims and permissions)
        var userClaims = await userManager.GetClaimsAsync(user);
        identity.AddClaims(userClaims);
        

        // Set destinations (which tokens the claims appear in)
        identity.SetDestinations(GetDestinations);

        var principal = new ClaimsPrincipal(identity);

        // Set requested scopes
        principal.SetScopes(scopes);

        // Set resources based on role scopes, permissions and requested scopes
        var allowedResources = GetResourcesFromUserClaims(scopes);
        principal.SetResources(allowedResources);

        return principal;
    }


    private static IEnumerable<string> GetResourcesFromUserClaims(
        ImmutableArray<string> requestedScopes)
    {
        var resources = new HashSet<string>();
        

        // Define API scope mappings
        var apiScopes = Constants.StandardScopes.GetApiScopes();

        // Check each API scope
        foreach (var apiScope in apiScopes)
        {
            if (requestedScopes.Contains(apiScope))
            {
                resources.Add(apiScope);
            }
        }

        return resources;
    }

    private static bool HasScopeAccess(HashSet<string> userScopes, string requiredScope)
    {
        // Check if user has the specific scope or a wildcard scope
        return userScopes.Contains(requiredScope)
               || userScopes.Contains("*")
               || userScopes.Contains("all");
    }


    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Determine which tokens should include this claim
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.PreferredUsername:
            case OpenIddictConstants.Claims.EmailVerified:
            case OpenIddictConstants.Claims.PhoneNumber:
            case "phone_number_country_code":
            case OpenIddictConstants.Claims.PhoneNumberVerified:
            case OpenIddictConstants.Claims.Zoneinfo:
            case Constants.ClaimTypes.TwoFactorEnabled:
            case "safe_browsing_mode":
                // Include in both access and identity tokens
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Role:
            case Constants.StandardScopes.RoleType:
                // Include role in both tokens for consistency
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case "AspNet.Identity.SecurityStamp":
                // Never include security stamp in tokens
                yield break;

            default:
                // Check if this is a permission claim
                if (claim.Type == Constants.StandardScopes.Permissions || claim.Type == "permission")
                {
                    // Permissions only in access token
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield break;
                }

                // Include other custom claims in access token by default
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }

    #region Helper Methods

    private IActionResult ForbidWithError(string error, string errorDescription)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
    }

    private static bool IsRecoverableIdentityError(Exception ex) =>
        ex is DbUpdateConcurrencyException
            or TimeoutException
            or OperationCanceledException
            or DbUpdateException; // Added for transient DB errors

    #endregion
}