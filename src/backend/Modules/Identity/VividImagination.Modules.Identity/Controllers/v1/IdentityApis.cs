using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Modules.Identity.Features.Auth.Commands;
using VividImagination.Modules.Identity.Features.Auth.Dtos;
using VividImagination.Modules.Identity.Features.Preferences.Commands;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Modules.Identity.Features.Preferences.Queries;
using VividImagination.Modules.Identity.Features.Users.Commands;
using VividImagination.Modules.Identity.Features.Users.Dtos;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Identity.Controllers.v1;

public static class IdentityApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapIdentityApiV1(this IEndpointRouteBuilder app)
    {
        var usersGroup = app
            .MapGroup($"{AppConstants.ApiRoute}{IdentityModule.ModuleConstants.ModuleNameForRouting}users")
            .WithTags(IdentityModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);

        usersGroup.MapPost("/register", RegisterUserAsync)
            .WithName(nameof(RegisterUserAsync))
            .WithDescription("Register a new user account with email and password")
            .AllowAnonymous()
            .Produces<RegisterUserResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        usersGroup.MapGet("/me", GetCurrentUserAsync)
            .WithName(nameof(GetCurrentUserAsync))
            .WithDescription("Get the current authenticated user profile")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<Profile>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        usersGroup.MapGet("/me/preferences", GetUserPreferencesAsync)
            .WithName(nameof(GetUserPreferencesAsync))
            .WithDescription("Get safe browsing preferences for current user")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<SafeBrowsingPreferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        usersGroup.MapPut("/me/preferences", UpdateUserPreferencesAsync)
            .WithName(nameof(UpdateUserPreferencesAsync))
            .WithDescription("Update safe browsing preferences for current user")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<SafeBrowsingPreferenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        var authGroup = app
            .MapGroup($"{AppConstants.ApiRoute}{IdentityModule.ModuleConstants.ModuleNameForRouting}auth")
            .WithTags(IdentityModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);

        authGroup.MapPost("/google", GoogleSignInAsync)
            .WithName(nameof(GoogleSignInAsync))
            .WithDescription("Sign in or auto-provision user with Google ID token")
            .AllowAnonymous()
            .Produces<GoogleSignInResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static IResult GetCurrentUserAsync(ICurrentUser currentUser)
    {
        return currentUser.CurrentUser == null ? throw new UnAuthorizedException("You are not authorized") : Results.Ok(currentUser.CurrentUser);
    }

    private static async Task<IResult> RegisterUserAsync(
        [FromBody] RegisterUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterUserCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.SafeBrowsingMode), ct);

        return Results.Created($"/api/v1/identity/users/{result.UserId}", result);
    }

    private static async Task<IResult> GoogleSignInAsync(
        [FromBody] GoogleSignInRequest request,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GoogleSignInCommand(
            request.IdToken,
            request.SafeBrowsingMode), ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserPreferencesAsync(
        [FromServices] ICurrentUser currentUser,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var userId = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized");
        var result = await mediator.Send(new GetSafeBrowsingPreferenceQuery(userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateUserPreferencesAsync(
        [FromBody] UpdatePreferencesRequest request,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var userId = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized");
        var result = await mediator.Send(new UpdateSafeBrowsingPreferenceCommand(userId, request.SafeBrowsingMode), ct);
        return Results.Ok(result);
    }
}
