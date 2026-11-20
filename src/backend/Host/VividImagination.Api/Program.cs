using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VividImagination.Modules.Feed;
using VividImagination.Modules.Identity;
using VividImagination.Modules.Media;
using VividImagination.Modules.Moderation;
using VividImagination.Modules.Social;
using VividImagination.Shared.Infrastructure;
using VividImagination.Shared.Infrastructure.Caching;
using VividImagination.Shared.Infrastructure.Cron;
using VividImagination.Shared.Infrastructure.Logging;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;
using VividImagination.Shared.Infrastructure.Mongo.Extensions;
using VividImagination.Shared.Infrastructure.Web;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var env = builder.Environment;


try
{
    builder.UseCustomSerilog(env);

    Log.Information("Configuring web host ({ApplicationContext})...", env.ApplicationName);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

    builder.Services.AddControllers()
        .AddRegisteredModulesToMvc()
        .ConfigureApiBehaviorOptions(options =>
        {
            //options.SuppressModelStateInvalidFilter = true;
            //Temporary Fix For Model State Validation
            options.InvalidModelStateResponseFactory = (context) =>
            {
                var modelStates = context.ModelState;

                var problemDetails = new ValidationProblemDetails(modelStates)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        })
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });


    builder.Services.AddProblemDetails();
    builder.Services.AddAntiforgery();

    builder.Services.AddCustomVersioning();

    builder.Services.AddHttpContextAccessor();

    #region Identity Context

    // ICurrentUser is registered via AddCustomAuthentication (CurrentWebUser)

    #endregion

    builder.Services
        .AddInfrastructure(configuration)
        .AddCustomConfiguration(configuration)
        .AddCustomCors(configuration)
        .AddDataProtectionServices(configuration, env.ApplicationName)
        .AddCaching(configuration)
        .AddCron(configuration);


    #region Auth

    builder.Services.AddCustomAuthentication(configuration);

    #endregion

    #region Swagger

    builder.Services.AddOpenApi(configuration);

    #endregion

    #region MongoDb DateOffset Serializer

    BsonMappings.Register();

    #endregion

    #region Module Services

    var modules = new[]
    {
        typeof(IdentityModule),
        typeof(FeedModule),
        typeof(MediaModule),
        typeof(ModerationModule),
        typeof(SocialModule),
    };
    builder.Services.AddModules(
        modules
    );

    #endregion

    #region Message Broker

    builder.Services.AddMessageBroker(configuration);

    #endregion


    #region Workers

    var clearCacheCronExpression = configuration[nameof(ClearCacheJob)];
    if (!string.IsNullOrEmpty(clearCacheCronExpression))
    {
        builder.Services.AddSingleton<ClearCacheJob>();
        builder.Services.AddCronJob<ClearCacheJob>(
            cronExpression: clearCacheCronExpression);
    }

    #endregion

    #region Permissions

    var moduleAssemblies = modules
        .Select(m => m.Assembly)
        .Distinct()
        .ToArray();
    builder.Services.AddPermissionProviders(moduleAssemblies)
        .AddScoped<StandardPermissionProvider>();

    #endregion


    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseExceptionHandler();
    app.UseCorrelationId();

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseCustomCors(configuration);
    // Configure the HTTP request pipeline.
    app.UseOpenApi(configuration);

    app.UseCustomAuthentication();
    app.UseAntiforgery();

    app.MapControllers();

    #region Module Endpoint

    app.UseModules();
    app.MapModuleEndpoints();

    #endregion

    app.MapGet("/", context =>
    {
        context.Response.Redirect("/docs");
        return Task.CompletedTask;
    });


    Log.Information("Starting web host ({ApplicationContext})...", env.ApplicationName);
    await app.RunAsync();

    Log.Information("Stopping web host ({ApplicationContext})...", env.ApplicationName);

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", env.ApplicationName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}