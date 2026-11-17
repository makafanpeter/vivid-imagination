using System.Reflection;
using Amazon.Rekognition;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Moderation.Controllers.v1;
using VividImagination.Modules.Moderation.Features.Reviews.Consumers;
using VividImagination.Modules.Moderation.Persistence;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Modules.Moderation.Services;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

namespace VividImagination.Modules.Moderation;

public class ModerationModule : IAppModule, IMessagingModule
{
    public string ModuleName => "Moderation";

    public void RegisterConsumers(IRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<PhotoUploadedConsumer>();
        cfg.AddConsumer<PhotoReportedConsumer>();
    }

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        
        var mongoSection = configuration.GetSection(ModuleName);
        services.AddMongoDbContext<ModerationDbContext>(mongoSection)
            .AddRepository<ModerationReview, Guid, ModerationDbContext>(ctx => ctx.Reviews);

        
        
        services.AddCustomMediatR(Assembly.GetExecutingAssembly());
        //services.AddCustomAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(typeof(ModerationModule).Assembly);

        services.AddAuthorizationPolicies(
            Assembly.GetExecutingAssembly(),
            Constants.StandardScopes.ModerationApi,
            []);
        
        // Register AWS Rekognition client if AWS region is configured, else null (triggers graceful fallback)
        var awsRegion = configuration["AWS:Region"];
        if (!string.IsNullOrEmpty(awsRegion))
        {
            services.AddSingleton<IAmazonRekognition>(_ =>
            {
                var region = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
                return new AmazonRekognitionClient(region);
            });
        }

        services.AddScoped<IAmazonRekognitionService, AmazonRekognitionService>();

        return services;
    }

    public IMvcBuilder AddModule(IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(ModerationModule).Assembly);
    }

    public IApplicationBuilder UseModule(IApplicationBuilder app)
    {
        return app;
    }

    public IEndpointRouteBuilder UseModuleApi(IEndpointRouteBuilder app)
    {
        var providerApi = app.NewVersionedApi(ModuleConstants.FormattedModuleName);
        providerApi.MapModerationApiV1();
        return app;
    }
    
    
    public static class ModuleConstants
    {
        public const string FormattedModuleName = "Moderation Module";
        public const string ModuleNameForRouting = "/moderation-service/";
    }
}
