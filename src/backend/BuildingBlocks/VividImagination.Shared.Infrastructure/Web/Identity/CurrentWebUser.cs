using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Web.Authorization;

namespace VividImagination.Shared.Infrastructure.Web.Identity;

public class CurrentWebUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private Profile? _currentUser;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? GetUserName()
    {
        return User?.FindFirst(OpenIddictConstants.Claims.PreferredUsername)?.Value
               ?? User?.FindFirst(OpenIddictConstants.Claims.Name)?.Value
               ?? User?.FindFirst(ClaimTypes.Name)?.Value
               ?? User?.Identity?.Name;
    }

    public Profile? CurrentUser
    {
        get
        {
            if (_currentUser != null)
            {
                return _currentUser;
            }

            if (!IsAuthenticated || User == null)
            {
                return null;
            }

            var safeBrowsingStr = User.FindFirst("safe_browsing_mode")?.Value;
            var safeBrowsingMode = Enum.TryParse<SafeBrowsingMode>(safeBrowsingStr, true, out var mode)
                ? mode
                : SafeBrowsingMode.BlurSensitive;

            var permissions = User.FindAll(Constants.StandardScopes.Permissions)
                .Concat(User.FindAll("permission"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            var scopes = User.FindAll(OpenIddictConstants.Claims.Scope)
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .ToList();

            _currentUser = new Profile
            {
                Sub = GetUserId()?.ToString() ?? User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ?? string.Empty,
                Email = User.FindFirst(OpenIddictConstants.Claims.Email)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                FullName = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                PreferredUsername = GetUserName() ?? string.Empty,
                Role = User.FindFirst(OpenIddictConstants.Claims.Role)?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                Permissions = permissions,
                Scopes = scopes,
                SafeBrowsingMode = safeBrowsingMode
            };

            return _currentUser;
        }
        set => _currentUser = value;
    }

    public long? GetUserId()
    {
        var subject = User?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
                      ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(subject, out var id) ? id : null;
    }

    public bool HasPermission(string permission)
    {
        return User != null && (
            User.HasClaim(Constants.StandardScopes.Permissions, permission) ||
            User.HasClaim("permission", permission));
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(HasPermission);
    }

    public bool HasAllPermissions(params string[] permissions)
    {
        return permissions.All(HasPermission);
    }

    public bool IsInRoleType(RoleType roleType)
    {
        return User?.IsInRoleType(roleType) ?? false;
    }

    public bool IsOwnerOf(string resourceOwnerId)
    {
        return GetUserId()?.ToString() == resourceOwnerId;
    }

    public bool IsInRole(string role)
    {
        return (User?.IsInRole(role) ?? false)
               || (User?.HasClaim(OpenIddictConstants.Claims.Role, role) ?? false);
    }

    public bool HasScope(string scope)
    {
        if (User == null) return false;

        var scopes = User.FindAll(OpenIddictConstants.Claims.Scope)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}
