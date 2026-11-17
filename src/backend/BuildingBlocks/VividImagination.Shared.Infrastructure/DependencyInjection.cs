using Microsoft.AspNetCore.Builder;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Domain;
using VividImagination.Shared.Domain.Entities;
using VividImagination.Shared.Domain.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Behaviours;
using VividImagination.Shared.Infrastructure.Web.Filters;
using VividImagination.Shared.Infrastructure.Web.Helpers;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Web.Identity;

namespace VividImagination.Shared.Infrastructure
{
   

    

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
      
        //web helper
        services.AddSingleton<IWebHelper, WebHelper>();
        services.AddScoped<IDateTimeHelper, DateTimeHelper>();
       
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddHttpContextAccessor();
        services.AddDistributedCache(configuration);
      
        
        return services;
    }
    
    
    
   public static IServiceCollection AddOpenApi(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
        
        var authProvider = applicationSettings.General.DefaultAuthProvider.ParseEnum<AuthProvider>();

        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Vivid Imagination Backend API",
                Description = "API for Vivid Imagination Backend Services",
                Contact = new OpenApiContact
                {
                    Name = "Vivid Imagination Team",
                    Email = "support@VividImagination.com"
                }
            });
            
            config.SwaggerDoc("v2", new OpenApiInfo 
            { 
               Title = "Vivid Imagination Backend API - V2", 
               Version = "v2" ,
               Description = "API for Vivid Imagination Backend Services",
               Contact = new OpenApiContact
               {
                  Name = "Vivid Imagination Team",
                  Email = "support@VividImagination.com"
               }
            });

            // Configure based on auth provider
            if (authProvider == AuthProvider.OpenIdConnect)
            {
                ConfigureOpenIddictSwagger(config, applicationSettings.OpenIdConnectSettings);
            }

            // Add XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                config.IncludeXmlComments(xmlPath);
            }

            // Enable annotations
            config.EnableAnnotations();

            // Custom operation filter to add auth info per endpoint
            config.OperationFilter<AuthorizeCheckOperationFilter>();
        });
        
        return services;
    }

    private static void ConfigureOpenIddictSwagger(
        SwaggerGenOptions config, 
        OpenIdConnectSettings settings)
    {
        var authority = settings.Authority; 

        config.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                // Password Flow (Resource Owner Password Credentials)
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{authority}/connect/token"),
                    Scopes = Constants.StandardScopes.GetAllScopesWithDescription()
                }
            },
            Description = "OAuth2 authentication via OpenIddict",
            Name = "oauth2",
            In = ParameterLocation.Header,
            Scheme = "Bearer"
        });
        
        
        config.AddSecurityRequirement(document =>
        {
            // Point to the already-defined "oauth2" scheme in Components.SecuritySchemes
            var schemeRef = new OpenApiSecuritySchemeReference("oauth2", document);

            var requirement = new OpenApiSecurityRequirement
            {
                [schemeRef] = new List<string>
                {
                    "openid",
                    "profile",
                    "email",
                    "roles",
                    
                }.Concat(Constants.StandardScopes.GetApiScopes()).ToList()
            };

            return requirement;
        });
        
    }

  


   public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app, IConfiguration configuration)
   {
      app.UseSwagger(options => { options.RouteTemplate = "docs/{documentName}/swagger.json"; });

      app.UseSwaggerUI(options =>
      {
         options.SwaggerEndpoint("/docs/v1/swagger.json", "Vivid Imagination API V1");
         options.SwaggerEndpoint("/docs/v2/swagger.json", "Vivid Imagination API V2"); // Ensure document name matches
         options.RoutePrefix = string.Empty; // Serves Swagger UI at root (/)
         
         
         var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>();

         if (settings?.General.DefaultAuthProvider.ParseEnum<AuthProvider>() == AuthProvider.OpenIdConnect)
         {
             var oidcSettings = settings.OpenIdConnectSettings;

             // OAuth2 configuration for Swagger UI
             options.OAuthClientId("swagger-ui");
             options.OAuthClientSecret(""); // Only if using confidential client
             options.OAuthAppName("Swagger UI");
             options.OAuthUsePkce(); // Use PKCE for better security

             // Pre-select scopes in Swagger UI
             options.OAuthScopes(
                [ "openid",
                   "profile",
                   "email",
                   "roles",
                   ..Constants.StandardScopes.GetApiScopes()
                ]
             );

             // Display authorization data in Swagger UI
             options.DisplayOperationId();
             options.DisplayRequestDuration();
         }

      });

      return app;
   }
   
   
   public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
   {
      var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
      
      var authProvider = applicationSettings.General.DefaultAuthProvider.ParseEnum<AuthProvider>();

      if (authProvider == AuthProvider.OpenIdConnect)
      {
         ConfigureOpenIdConnectAuthentication(services, configuration);
      }

      services.AddScoped<ICurrentUser, CurrentWebUser>();

      return services;
   }

   private static void ConfigureOpenIdConnectAuthentication(IServiceCollection services, IConfiguration configuration)
   {
      var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
      
      services.AddAuthentication(options =>
      {
         options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
         options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
         options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
      });
      
      if (applicationSettings.OpenIdConnectSettings.EnableValidation)
      {
         services.AddOpenIddict()
            .AddValidation(options =>
            {
               if (!string.IsNullOrWhiteSpace(applicationSettings.OpenIdConnectSettings?.Authority))
               {
                  options.SetIssuer(new Uri(applicationSettings.OpenIdConnectSettings.Authority));
               }
               
               if (applicationSettings.OpenIdConnectSettings != null && applicationSettings.OpenIdConnectSettings.Audiences.Length != 0)
               {
                  options.AddAudiences(applicationSettings.OpenIdConnectSettings.Audiences);
               }

               options.UseSystemNetHttp();
               options.UseAspNetCore();
            });
      }
      
         
   }

  


   public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder app)
   {
      app.UseAuthentication();
      app.UseAuthorization();
      
      return app;
   }


   public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
   {
      var appSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
      services.AddCors(options =>
      {
         options.AddPolicy("AllowedOrigins", builder => builder
            .WithOrigins(appSettings.CORS.AllowedOrigins ?? [])
            .AllowAnyMethod()
            .AllowAnyHeader());

         options.AddPolicy("AllowAnyOrigin", builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

         options.AddPolicy("CustomPolicy", builder => builder
            .AllowAnyOrigin()
            .WithMethods("Get")
            .WithHeaders("Content-Type"));
      });
      
      
      return services;

   }
   
   
   
   public static IServiceCollection AddCustomConfiguration(this IServiceCollection services, IConfiguration configuration)
   {

      //Settings
      services.Configure<ApplicationSettings>(configuration.GetSection(nameof(ApplicationSettings)));
      services.AddOptions();
      return services;
   }


   public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app, IConfiguration configuration)
   {
      var appSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));
      app.UseCors(appSettings.CORS.AllowAnyOrigin ? "AllowAnyOrigin" : "AllowedOrigins");

      return app;
   }
   
   
   public static IServiceCollection AddCustomMediatR(this IServiceCollection services, params Assembly [] assemblies)
   {
      services.AddMediatR(cfg =>
      {
         cfg.RegisterServicesFromAssemblies(assemblies);
         
         cfg.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
         cfg.AddOpenBehavior(typeof(RequestPerformanceBehaviour<,>));
         cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
         cfg.AddOpenBehavior(typeof(InvalidateCachingBehavior<,>));
      });
      
      return services;
   }
   
   




   
   public static IServiceCollection AddMongoDbContext<TContext>(
      this IServiceCollection services, IConfiguration configuration, Action<MongoOptions>? configurator = null)
      where TContext : MongoDbContext
   {
      return services.AddMongoDbContext<TContext, TContext>(configuration, configurator);
   }

   public static IServiceCollection AddMongoDbContext<TContextService, TContextImplementation>(
      this IServiceCollection services, IConfiguration configuration, Action<MongoOptions>? configurator = null)
      where TContextService : class, IMongoDbContext
      where TContextImplementation : MongoDbContext, TContextService
   {
      var contextName = typeof(TContextImplementation).Name;
      
      var mongoSection = configuration.GetSection(nameof(MongoOptions));
      
      services.Configure<MongoOptions>(contextName, mongoSection);

      if (configurator is not null)
      {
         services.Configure(contextName, configurator);
      }
      
      services.AddScoped<TContextService>(sp =>
      {
         var optionsSnapshot = sp.GetRequiredService<IOptionsSnapshot<MongoOptions>>();
         var options = optionsSnapshot.Get(contextName);
         var optionsWrapper = Options.Create(options);
         return (TContextService)Activator.CreateInstance(typeof(TContextImplementation), optionsWrapper)!;
      });
      
      services.AddScoped<IMongoDbContext>(sp => sp.GetRequiredService<TContextService>());
      
      
      services.AddHostedService<DbContextInitializer<TContextImplementation>>();
      
      services.AddTransient(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));
      services.AddTransient(typeof(IMongoUnitOfWork<>), typeof(MongoUnitOfWork<>));

      return services;
   }
   
   public static IServiceCollection AddRepository<TEntity, TId, TContext>(
      this IServiceCollection services,
      Func<TContext, IMongoCollection<TEntity>> collectionSelector)
      where TEntity : class, IAggregate<TId>
      where TContext : MongoDbContext
   {
      services.AddScoped<IMongoRepository<TEntity, TId>>(sp =>
      {
         var context = sp.GetRequiredService<TContext>();
         var collection = collectionSelector(context);
         return new MongoRepository<TEntity, TId>(collection);
      });

      return services;
   }
   
   public static IServiceCollection AddDataProtectionServices(
      this IServiceCollection services,
      IConfiguration configuration,
      string appName)
   {
      var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));

      var dataProtectionSettings =  applicationSettings.DataProtectionSettings;

      if (!Enum.TryParse<DataProtectionSettings.DataProtectionStorage>(dataProtectionSettings.Storage, ignoreCase: true, out var storageType))
      {
         storageType = DataProtectionSettings.DataProtectionStorage.Local;
      }

      var builder = services.AddDataProtection()
         .SetApplicationName(appName);

      switch (storageType)
      {
         case DataProtectionSettings.DataProtectionStorage.Local:
         default:
         {
            var localFolder = Path.Combine(string.IsNullOrEmpty(dataProtectionSettings.LocalKeysPath) ? AppContext.BaseDirectory : dataProtectionSettings.LocalKeysPath, "keys");
            Directory.CreateDirectory(localFolder);
            builder.PersistKeysToFileSystem(new DirectoryInfo(localFolder));
            break;
         }
      }

      return services;
   }


   private static void AddDistributedCache(this IServiceCollection services, IConfiguration configuration)
   {
            
      var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
         .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));     
        
        
      var distributedCacheType = applicationSettings.Cache.DistributedCache.ParseEnum<DistributedCacheType>();

      if (!applicationSettings.Cache.EnableDistributedCache)
         return;

      switch (distributedCacheType)
      {
         case DistributedCacheType.Memory:
         default:
            services.AddDistributedMemoryCache();
            break;
         case DistributedCacheType.Redis:
            services.AddStackExchangeRedisCache(options =>
            {
               options.Configuration = applicationSettings.Cache.ConnectionString;
            });
            break;
      }
   }

   
   

}
}
