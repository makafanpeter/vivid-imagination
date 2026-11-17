using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;

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
       
    }
}