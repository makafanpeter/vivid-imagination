using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VividImagination.Shared.Infrastructure.Web.Filters;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType != null &&
                           (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any());

        var hasAllowAnonymous = context.MethodInfo.DeclaringType != null &&
                                (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
                                 context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any());

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses ??=  new OpenApiResponses();
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            // Reference the existing oauth2 scheme by id
            var schemeRef = new OpenApiSecuritySchemeReference("oauth2", context.Document);

            operation.Security ??= new List<OpenApiSecurityRequirement>();

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [schemeRef] = new List<string>()
            });
        }
    }
}

