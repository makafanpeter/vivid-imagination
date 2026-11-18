using System.Security.Claims;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Shared.Contract.Identity;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    ClaimsPrincipal? User { get; }
    string? GetUserName();
    Profile? CurrentUser { get; set; }
    long? GetUserId();
    
    // Helper methods
    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    bool HasAllPermissions(params string[] permissions);
    bool IsInRoleType(RoleType roleType);
    bool IsOwnerOf(string resourceOwnerId);
    bool IsInRole(string role);
    bool HasScope(string scope);
}
