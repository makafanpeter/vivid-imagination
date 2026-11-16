using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Identity.Persistence;
using VividImagination.Shared.Infrastructure.Modules;

namespace VividImagination.Modules.Identity;

public class IdentityModule : IAppModule
{
    public string ModuleName => "Identity";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        

        return services;
    }

    public IMvcBuilder AddModule(IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(IdentityModule).Assembly);
    }

    public IApplicationBuilder UseModule(IApplicationBuilder app)
    {
        return app;
    }

    public IEndpointRouteBuilder UseModuleApi(IEndpointRouteBuilder app)
    {
        return app;
    }
}
