using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Shared.Domain;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.Modules;

namespace VividImagination.Shared.Infrastructure.Messaging;

public static class Extensions
{
    private static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
        var rabbitMqSettings = applicationSettings.EventBus.RabbitMQ;
        

        services.AddMassTransit(x =>
        {
            RegisterConsumersFromModules(x, services);
            x.AddDelayedMessageScheduler();

            x.UsingRabbitMq((context, cfg) =>
            {
                RabbitMqHostConfigurator.Configure(cfg, rabbitMqSettings);
                cfg.UseDelayedMessageScheduler();
                
                cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter($"{AppConstants.AppName}.", false));
            });
        });

        return services;
    }


    private static IServiceCollection AddAWS_SQS(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        
        var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));

        var sqsSettings = applicationSettings.EventBus.SQS ?? throw new ArgumentNullException(nameof(SQSSettings));

        services.AddMassTransit(x =>
        {
            RegisterConsumersFromModules(x, services);
            x.AddDelayedMessageScheduler();

            x.UsingAmazonSqs((context, cfg) =>
            {
                SqsHostConfigurator.Configure(cfg, sqsSettings, environment);
                cfg.UseDelayedMessageScheduler();
                    
                // Configure endpoints for each module
                ConfigureEndpointsFromModules(cfg, context, environment, services);
            });
        });

        return services;
    }
    
    
    private static IServiceCollection AddInMemory(
        this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            RegisterConsumersFromModules(x, services);
            x.AddDelayedMessageScheduler();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseDelayedMessageScheduler();
                cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter($"{AppConstants.AppName}.", false));
            });
        });

        return services;
    }

    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment = null)
    {
        var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
        var brokerType = applicationSettings.General.MessageBrokerType.ParseEnum<MessageBrokerType>();

        return brokerType switch
        {
            MessageBrokerType.RabbitMq => services.AddRabbitMq(configuration),
            MessageBrokerType.AmazonSqs => services.AddAWS_SQS(configuration, webHostEnvironment),
            MessageBrokerType.InMemory => services.AddInMemory(),
            _ => throw new InvalidOperationException($"Unsupported message broker type: {brokerType}")
        };
    }
    
    
    
    private static void RegisterConsumersFromModules(
        IRegistrationConfigurator cfg,
        IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var modules = AppModuleExtensions.InitializedModules
            .Select(type => sp.GetService(type) as IAppModule)
            .OfType<IMessagingModule>()
            .ToList();

        Console.WriteLine($"📨 Registering consumers from {modules.Count} messaging modules...");

        foreach (var module in modules)
        {
            Console.WriteLine($"   └─ {((IAppModule)module).ModuleName}");
            module.RegisterConsumers(cfg);
        }
    }
    
    
    private static void ConfigureEndpointsFromModules(
        IAmazonSqsBusFactoryConfigurator cfg,
        IBusRegistrationContext context,
        IHostEnvironment environment,
        IServiceCollection services)
    {
        // SQS requires explicit endpoint configuration
        // This can be enhanced based on your needs
    }
    
    
    public static (Uri Execute, Uri Compensate) GetActivityUris<TActivity, TArguments>()
        where TActivity : IExecuteActivity<TArguments>
        where TArguments : class
    {
        var activityName = typeof(TActivity).Name.Replace("Activity", "");
        var kebabCaseName = ToKebabCase(activityName);
        
        var executeUri = new Uri($"queue:{kebabCaseName}_execute");
        var compensateUri = new Uri($"queue:{kebabCaseName}_compensate");
        
        return (executeUri, compensateUri);
    }
    
    public static (Uri Execute, Uri Compensate) GetActivityUrisFromEndpoint(
        string endpointName)
    {
        var executeUri = new Uri($"queue:{endpointName}-execute");
        var compensateUri = new Uri($"queue:{endpointName}-compensate");
        
        return (executeUri, compensateUri);
    }
    
    public static (string Execute,string Compensate) GetActivityEndpoint(
        string endpointName)
    {
        var executeEndpointName = $"{endpointName}-execute";
        var compensateEndpointName = $"{endpointName}-compensate";
        
        return (executeEndpointName, compensateEndpointName);
    }
    

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(
                value,
                "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])",
                "-$1",
                RegexOptions.Compiled)
            .Trim()
            .ToLower();
    }

}