using System.Reflection;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Modules.Feed.Controllers.v1;
using VividImagination.Modules.Feed.Features.Feeds.Consumers;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

namespace VividImagination.Modules.Feed;

public class FeedModule : IAppModule, IMessagingModule
{
    public string ModuleName => "Feed";

    public void RegisterConsumers(IRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<PhotoUploadedFeedConsumer>();
        cfg.AddConsumer<PhotoModeratedFeedConsumer>();
        cfg.AddConsumer<PhotoOptimizedFeedConsumer>();
        cfg.AddConsumer<PhotoVotedFeedConsumer>();
        cfg.AddConsumer<PhotoDeletedFeedConsumer>();
    }

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var mongoSection = configuration.GetSection($"{ModuleName}");
        
        services.AddMongoDbContext<FeedDbContext>(mongoSection)
            .AddRepository<FeedItem, Guid, FeedDbContext>(ctx => ctx.FeedItems);

        services.AddCustomMediatR(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(typeof(FeedModule).Assembly);
        
        
        services.AddAuthorizationPolicies(
            Assembly.GetExecutingAssembly(),
            Constants.StandardScopes.FeedApi,
            []);

        return services;
    }

    public IMvcBuilder AddModule(IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(FeedModule).Assembly);
    }

    public IApplicationBuilder UseModule(IApplicationBuilder app)
    {
        return app;
    }

    public IEndpointRouteBuilder UseModuleApi(IEndpointRouteBuilder app)
    {
        var builder = app.NewVersionedApi(ModuleConstants.FormattedModuleName);
        builder.MapFeedApiV1();
        return app;
    }
    
    public static class ModuleConstants
    {
        public const string FormattedModuleName = "Feed Module";
        public const string FormatedModuleName = FormattedModuleName;
        public const string ModuleNameForRouting = "/feed";
    }
}
