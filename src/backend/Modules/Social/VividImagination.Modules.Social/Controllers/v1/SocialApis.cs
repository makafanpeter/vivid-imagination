using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Modules.Social.Features.Reports.Commands;
using VividImagination.Modules.Social.Features.Reports.Dtos;
using VividImagination.Modules.Social.Features.Reports.Queries;
using VividImagination.Modules.Social.Features.Votes.Commands;
using VividImagination.Modules.Social.Features.Votes.Dtos;
using VividImagination.Modules.Social.Features.Votes.Queries;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Modules.Social.Controllers.v1;

public static class SocialApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapSocialApiV1(this IEndpointRouteBuilder app)
    {
        MapSocialRoutes(app);
        return app;
    }

    private static void MapSocialRoutes(IEndpointRouteBuilder app)
    {
        var api = app
            .MapGroup($"{AppConstants.ApiRoute}{SocialModule.ModuleConstants.ModuleNameForRouting}photos/{{photoId:guid}}")
            .WithTags(SocialModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);
        
        api.MapPost("/vote", VoteOnPhotoAsync)
            .WithName(nameof(VoteOnPhotoAsync))
            .WithDescription("Vote on a photo")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<VoteResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        api.MapGet("/vote", GetVoteOnPhotoAsync)
            .WithName(nameof(GetVoteOnPhotoAsync))
            .WithDescription("Get the current vote for a photo")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<VoteResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        api.MapPost("/report", ReportPhotoAsync)
            .WithName(nameof(ReportPhotoAsync))
            .WithDescription("Report a photo")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<ReportResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        api.MapGet("/report", GetPhotoReportAsync)
            .WithName(nameof(GetPhotoReportAsync))
            .WithDescription("Get the report status for a photo")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<ReportResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
    
    private static async Task<IResult> VoteOnPhotoAsync(
        [FromRoute] Guid photoId,
        [FromBody] VoteRequest request,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new VoteOnPhotoCommand(photoId, request.Value), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetVoteOnPhotoAsync(
        [FromRoute] Guid photoId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetVoteOnPhotoQuery(photoId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ReportPhotoAsync(
        [FromRoute] Guid photoId,
        [FromBody] ReportRequest? request,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ReportPhotoCommand(photoId, request?.Reason), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPhotoReportAsync(
        [FromRoute] Guid photoId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPhotoReportQuery(photoId), ct);
        return Results.Ok(result);
    }
}
