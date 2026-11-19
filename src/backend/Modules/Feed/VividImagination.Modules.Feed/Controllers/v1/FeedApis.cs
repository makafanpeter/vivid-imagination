using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Features.Feeds.Queries;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Modules.Feed.Controllers.v1;

public static class FeedApis
{
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
            .Produces<IReadOnlyList<FeedItemDto>>(StatusCodes.Status200OK)
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
        [FromServices] IMediator mediator = default!,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPersonalizedFeedQuery(page, pageSize, safeBrowsingMode), ct);
        return Results.Ok(result);
    }
}
