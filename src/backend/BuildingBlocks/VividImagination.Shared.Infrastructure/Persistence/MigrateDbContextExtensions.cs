using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VividImagination.Shared.Infrastructure.Persistence;

public static class MigrateDbContextExtensions
{
    private static readonly string ActivitySourceName = "DbMigrations";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static IServiceCollection AddMigration<TContext>(this IServiceCollection services)
        where TContext : DbContext
        => services.AddMigration<TContext>((_, _) => Task.CompletedTask);

    public static IServiceCollection AddMigration<TContext>(this IServiceCollection services, Func<TContext, IServiceProvider, Task> seeder)
        where TContext : DbContext
    {
        // Enable migration tracing
        //services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(ActivitySourceName));

        return services.AddHostedService(sp => new MigrationHostedService<TContext>(sp, seeder));
    }

    public static IServiceCollection AddMigration<TContext, TDbSeeder>(this IServiceCollection services)
        where TContext : DbContext
        where TDbSeeder : class, IDbSeeder<TContext>
    {
        services.AddScoped<IDbSeeder<TContext>, TDbSeeder>();
        return services.AddMigration<TContext>((context, sp) => sp.GetRequiredService<IDbSeeder<TContext>>().SeedAsync(context));
    }

    private static async Task MigrateDbContextAsync<TContext>(this IServiceProvider services, Func<TContext, IServiceProvider, Task> seeder) where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var scopeServices = scope.ServiceProvider;
        var logger = scopeServices.GetRequiredService<ILogger<TContext>>();
        var context = scopeServices.GetService<TContext>();

        if (context == null)
        {
            logger.LogWarning("DbContext {DbContextName} was not found in the service provider. Skipping migration.", typeof(TContext).Name);
            return;
        }

        var configuration = scopeServices.GetService<IConfiguration>();
        if (!ShouldApplyMigrations<TContext>(configuration))
        {
            logger.LogInformation("Database migrations disabled by configuration for context {DbContextName}. Skipping.", typeof(TContext).Name);
            return;
        }

        if (!context.Database.IsRelational())
        {
            logger.LogInformation("Database context {DbContextName} is not a relational database. Skipping migrations.", typeof(TContext).Name);
            return;
        }

        using var activity = ActivitySource.StartActivity($"Migration operation {typeof(TContext).Name}");

        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(() => InvokeSeeder(seeder, context, scopeServices));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

            //activity.SetExceptionTags(ex);

            throw;
        }
    }

    public static bool ShouldApplyMigrations<TContext>(this IConfiguration? configuration) where TContext : DbContext
    {
        if (configuration == null)
        {
            return true;
        }

        var contextName = typeof(TContext).Name;

        // 1. Direct match: Database:Migrations:<ContextName> or Database:ApplyMigrations:<ContextName>
        var specificConfig = configuration.GetValue<bool?>($"Database:Migrations:{contextName}")
                          ?? configuration.GetValue<bool?>($"Database:ApplyMigrations:{contextName}");
        if (specificConfig.HasValue)
        {
            return specificConfig.Value;
        }

        // 2. Short name match: "Identity" for "IdentityContext", "Booking" for "BookingDbContext"
        var shortName = contextName;
        if (shortName.EndsWith("DbContext", StringComparison.OrdinalIgnoreCase))
        {
            shortName = shortName[..^9];
        }
        else if (shortName.EndsWith("Context", StringComparison.OrdinalIgnoreCase))
        {
            shortName = shortName[..^7];
        }

        if (!string.Equals(shortName, contextName, StringComparison.OrdinalIgnoreCase))
        {
            var shortConfig = configuration.GetValue<bool?>($"Database:Migrations:{shortName}")
                           ?? configuration.GetValue<bool?>($"Database:ApplyMigrations:{shortName}");
            if (shortConfig.HasValue)
            {
                return shortConfig.Value;
            }
        }

        // 3. Fallback to default/global: Database:Migrations:Default or Database:ApplyMigrations
        var globalConfig = configuration.GetValue<bool?>("Database:Migrations:Default")
                        ?? configuration.GetValue<bool?>("Database:ApplyMigrations");
        if (globalConfig.HasValue)
        {
            return globalConfig.Value;
        }

        return true;
    }

    private static async Task InvokeSeeder<TContext>(Func<TContext, IServiceProvider, Task> seeder, TContext context, IServiceProvider services)
        where TContext : DbContext
    {
        using var activity = ActivitySource.StartActivity($"Migrating {typeof(TContext).Name}");

        try
        {
            await context.Database.MigrateAsync();
            await seeder(context, services);
        }
        catch (System.Exception ex)
        {
            //activity.SetExceptionTags(ex);

            throw;
        }
    }

    private class MigrationHostedService<TContext>(IServiceProvider serviceProvider, Func<TContext, IServiceProvider, Task> seeder)
        : BackgroundService where TContext : DbContext
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return serviceProvider.MigrateDbContextAsync(seeder);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}