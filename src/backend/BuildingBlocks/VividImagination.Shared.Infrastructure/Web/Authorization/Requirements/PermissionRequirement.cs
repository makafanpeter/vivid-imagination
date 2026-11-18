using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VividImagination.Shared.Contract.Identity;

namespace VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; set; } = string.Empty;
    
    public string Scope { get; set; } = string.Empty;
}

public class PermissionRequirementHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        
        var currentUser = httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<ICurrentUser>();
        if (currentUser is not { IsAuthenticated: true })
        {
            context.Fail(new AuthorizationFailureReason(
                this, 
                "User is not authenticated"));
            return Task.CompletedTask;
        }
        
        if (!currentUser.HasPermission(requirement.PermissionName))
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                $"User lacks required permission: {requirement.PermissionName}"));
            return Task.CompletedTask;
        }

        if (!currentUser.HasScope(requirement.Scope))
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                $"User lacks required scope: {requirement.Scope}"));
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
