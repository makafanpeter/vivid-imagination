using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Authorization;

public class FileEntryAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, FileEntry>
{

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, FileEntry resource)
    {
        
        var currentUser = httpContextAccessor.HttpContext?
            .RequestServices
            .GetRequiredService<ICurrentUser>();
        
        if (currentUser is not { IsAuthenticated: true })
        {
            return Task.CompletedTask;
        }

        // Owner can do anything
        if (currentUser.IsOwnerOf(resource.OwnerId ?? string.Empty))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        //Anybody can read/view
        if ( requirement == Operations.Read)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Authorization failed
        context.Fail(new AuthorizationFailureReason(
            this,
            $"User does not have permission to {requirement.Name} this file"));

        return Task.CompletedTask;
    }
    
    
    private bool IsSameOrganization(string? userOrgId, string? resourceOrgId)
    {
        return !string.IsNullOrEmpty(userOrgId) && 
               !string.IsNullOrEmpty(resourceOrgId) &&
               userOrgId == resourceOrgId;
    }
}
