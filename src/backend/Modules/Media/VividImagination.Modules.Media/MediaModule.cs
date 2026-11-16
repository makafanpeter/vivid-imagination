using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Media.Persistence;
using VividImagination.Shared.Infrastructure.Modules;

namespace VividImagination.Modules.Media;

public class MediaModule : IAppModule
{
    public string ModuleName => "Media";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        

        return services;
    }

    public IMvcBuilder AddModule(IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(MediaModule).Assembly);
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
