using System.Security.Claims;
using Duende.IdentityModel;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Shared.Infrastructure.Web.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(JwtClaimTypes.Subject)?.Value 
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }


    public static RoleType? GetRoleType(this ClaimsPrincipal user)
    {
        var roleTypeStr = user.FindFirst(Constants.StandardScopes.RoleType)?.Value;
        return Enum.TryParse<RoleType>(roleTypeStr, out var roleType) 
            ? roleType 
            : null;
    }

    public static bool IsInRoleType(this ClaimsPrincipal user, RoleType roleType)
    {
        return user.GetRoleType() == roleType;
    }

    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.HasClaim(Constants.StandardScopes.Permissions, permission);
    }

    public static bool IsOwnerOf(this ClaimsPrincipal user, string resourceOwnerId)
    {
        return user.GetUserId() == resourceOwnerId;
    }
}
