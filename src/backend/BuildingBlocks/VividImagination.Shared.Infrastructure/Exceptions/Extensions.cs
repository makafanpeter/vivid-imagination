using Microsoft.Extensions.DependencyInjection;

namespace VividImagination.Shared.Infrastructure.Exceptions;

public static class Extensions
{
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(x => { });


        return services;
    }

}