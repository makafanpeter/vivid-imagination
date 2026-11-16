using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Consumers;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;

namespace VividImagination.Modules.Feed;

public class FeedModule : IAppModule, IMessagingModule
{
    public string ModuleName => "Feed";

    public void RegisterConsumers(IRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<PhotoUploadedFeedConsumer>();
        cfg.AddConsumer<PhotoModeratedFeedConsumer>();
        cfg.AddConsumer<PhotoVotedFeedConsumer>();
        cfg.AddConsumer<PhotoDeletedFeedConsumer>();
    }

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
       

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
        return app;
    }
}
