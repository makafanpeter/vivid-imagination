using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Validation.AspNetCore;

namespace VividImagination.Modules.Identity.Controllers.v1;

public static class IdentityApis
{
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };
     public static IEndpointRouteBuilder MapIdentityApiV1(this IEndpointRouteBuilder app)
     {
        
        
         return app;
     }

    
    
}