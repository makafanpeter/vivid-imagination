using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Modules.Moderation.Features.Reviews.Queries;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Modules.Moderation.Controllers.v1;

public static class ModerationApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapModerationApiV1(this IEndpointRouteBuilder app)
    {
        MapModerationRoutes(app);
        return app;
    }

    private static void MapModerationRoutes(IEndpointRouteBuilder app)
    {
        var api = app
            .MapGroup($"{AppConstants.ApiRoute}{ModerationModule.ModuleConstants.ModuleNameForRouting}reviews/{{photoId:guid}}")
            .WithTags(ModerationModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);
        
        
        api.MapGet("", GetModerationReviewAsync)
            .WithName(nameof(GetModerationReviewAsync))
            .WithDescription("Get a moderation review for a photo")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<ModerationReview>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        
    }
    
    private static async Task<IResult> GetModerationReviewAsync(
        [FromRoute] Guid photoId,
        [FromServices]IMediator mediator,
        CancellationToken ct)
    {
        var review = await mediator.Send(new GetModerationReviewQuery(photoId), ct);

        return Results.Ok(review);
    }
    
   
}