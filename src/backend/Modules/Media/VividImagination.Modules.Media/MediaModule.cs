using System.Reflection;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Media.Controllers.v1;
using VividImagination.Modules.Media.Features.Photos.Consumers;
using VividImagination.Modules.Media.Persistence;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services;
using VividImagination.Modules.Media.Services.ImageProcessing;
using VividImagination.Shared.Application;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

namespace VividImagination.Modules.Media;

public class MediaModule : IAppModule, IMessagingModule
{
    public string ModuleName => "Media";

    public void RegisterConsumers(IRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<PhotoOptimizationConsumer>();
        cfg.AddConsumer<ValidatePhotoConsumer>();
    }

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var mongoSection = configuration.GetSection($"{ModuleName}");
        
        services.AddMongoDbContext<MediaDbContext>(mongoSection)
            .AddRepository<FileEntry, Guid, MediaDbContext>(ctx => ctx.FileEntries);
        
        // Add the file service manager
        services.AddFileStorage(configuration);

        // Add Image processing service
        services.AddScoped<IImageProcessingService, ImageSharpProcessingService>();

        services.AddCustomMediatR(Assembly.GetExecutingAssembly());
        services.AddCustomAutoMapper(Assembly.GetExecutingAssembly());
        
        services.AddValidatorsFromAssembly(typeof(MediaModule).Assembly);
        
        services.AddAuthorizationPolicies(Assembly.GetExecutingAssembly(), Constants.StandardScopes.MediaApi, []);

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
        var builder = app.NewVersionedApi(ModuleConstants.FormattedModuleName);
        builder.MapMediaApiV1();
        return app;
    }
    
    public static class ModuleConstants
    {
        public const string FormattedModuleName = "Media Module";
        public const string FormatedModuleName = FormattedModuleName;
        public const string ModuleNameForRouting = "/media/";
    }
}
