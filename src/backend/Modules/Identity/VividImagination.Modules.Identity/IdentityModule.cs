using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Identity.Controllers.v1;
using VividImagination.Modules.Identity.Features.Auth.Services;
using VividImagination.Modules.Identity.Persistence;
using VividImagination.Modules.Identity.Persistence.Seed;
using VividImagination.Modules.Identity.Services;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.IdsGenerator;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Persistence;
using VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

namespace VividImagination.Modules.Identity;

public class IdentityModule : IAppModule
{
    public string ModuleName => "Identity";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddPersistence<IdentityDbContext>(ModuleName, configuration);
        services.AddMigration<IdentityDbContext, IdentityContextSeed>();
        
        services.AddCustomIdentityServer(environment, configuration);
        services.AddCustomMediatR(Assembly.GetExecutingAssembly());
        //services.AddCustomAutoMapper(Assembly.GetExecutingAssembly());
        
        SnowFlakIdGenerator.Configure(1);
        
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        
        services.AddValidatorsFromAssembly(typeof(IdentityModule).Assembly);
        
        services.AddAuthorizationPolicies(Assembly.GetExecutingAssembly(),  Constants.StandardScopes.IdentityApi, []);

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
        var identity = app.NewVersionedApi(ModuleConstants.FormattedModuleName);
        identity.MapIdentityApiV1();
        return app;
    }
    
    public static class ModuleConstants
    {
        public const string FormattedModuleName = "Identity Module";
        public const string FormatedModuleName = FormattedModuleName;
        public const string ModuleNameForRouting = "/identity/";
    }
    
    
}
