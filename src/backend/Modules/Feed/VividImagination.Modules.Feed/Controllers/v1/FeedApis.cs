using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Features.Feeds.Queries;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Feed.Controllers.v1;

public static class FeedApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapFeedApiV1(this IEndpointRouteBuilder app)
    {
        MapFeedRoutes(app);
        return app;
    }

    private static void MapFeedRoutes(IEndpointRouteBuilder app)
    {
        var api = app
            .MapGroup($"{AppConstants.ApiRoute}{FeedModule.ModuleConstants.ModuleNameForRouting}")
            .WithTags(FeedModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);

        api.MapGet("", GetRankedFeedAsync)
            .WithName(nameof(GetRankedFeedAsync))
            .WithDescription("Retrieve ranked public feed")
            .Produces<IReadOnlyList<FeedItemDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        api.MapGet("/personalized", GetPersonalizedFeedAsync)
            .WithName(nameof(GetPersonalizedFeedAsync))
            .WithDescription("Retrieve personalized feed with safe-browsing filters and excluded downvotes")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<IReadOnlyList<FeedItemDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        api.MapPost("/interactions", RecordInteractionAsync)
            .WithName(nameof(RecordInteractionAsync))
            .WithDescription("Record feed interaction telemetry (dwell time, share, hide, impression)")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetRankedFeedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SafeBrowsingMode? safeBrowsingMode = null,
        [FromServices] IMediator mediator = default!,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFeedQuery(page, pageSize, safeBrowsingMode), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPersonalizedFeedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SafeBrowsingMode? safeBrowsingMode = null,
        [FromServices] ICurrentUser currentUser = default!,
        [FromServices] IMediator mediator = default!,
        CancellationToken ct = default)
    {
        var userIdLong = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized");
        var userId = userIdLong.ToGuid();
        var result = await mediator.Send(new GetPersonalizedFeedQuery(page, pageSize, safeBrowsingMode, null, userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> RecordInteractionAsync(
        [FromBody] RecordInteractionRequest request,
        [FromServices] ICurrentUser currentUser = default!,
        [FromServices] IMediator mediator = default!,
        CancellationToken ct = default)
    {
        var userIdLong = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized");
        var userId = userIdLong.ToGuid();
        await mediator.Send(new RecordFeedInteractionCommand(
            userId,
            request.PhotoId,
            request.CreatorId,
            request.InteractionType,
            request.FeedPosition,
            request.DwellDurationMs,
            request.Tags), ct);

        return Results.Accepted();
    }
}
