using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Social.Controllers.v1;
using VividImagination.Modules.Social.Persistence;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Persistence;
using VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

namespace VividImagination.Modules.Social;

public class SocialModule : IAppModule
{
    public string ModuleName => "Social";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddPersistence<SocialDbContext>(ModuleName, configuration);
        services.AddMigration<SocialDbContext>();
        
        
        services.AddCustomMediatR(Assembly.GetExecutingAssembly());
        //services.AddCustomAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(typeof(SocialModule).Assembly);
        
        
        services.AddAuthorizationPolicies(
            Assembly.GetExecutingAssembly(),
            Constants.StandardScopes.ReputationApi,
            []);


        return services;
    }

    public IMvcBuilder AddModule(IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(SocialModule).Assembly);
    }

    public IApplicationBuilder UseModule(IApplicationBuilder app)
    {
        return app;
    }

    public IEndpointRouteBuilder UseModuleApi(IEndpointRouteBuilder app)
    {
        var providerApi = app.NewVersionedApi(ModuleConstants.FormattedModuleName);
        providerApi.MapSocialApiV1();
        return app;
    }
    
    public static class ModuleConstants
    {
        public const string FormattedModuleName = "Social Module";
        public const string ModuleNameForRouting = "/social-service/";
    }
}
