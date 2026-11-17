using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Modules.Identity.Controllers.v1;

public static class IdentityApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapIdentityApiV1(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup($"{AppConstants.ApiRoute}{IdentityModule.ModuleConstants.ModuleNameForRouting}users")
            .WithTags(IdentityModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName(nameof(GetCurrentUserAsync))
            .WithDescription("Get the current authenticated user profile")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<Profile>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static IResult GetCurrentUserAsync(ICurrentUser currentUser)
    {
        if (currentUser.CurrentUser == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(currentUser.CurrentUser);
    }
}
