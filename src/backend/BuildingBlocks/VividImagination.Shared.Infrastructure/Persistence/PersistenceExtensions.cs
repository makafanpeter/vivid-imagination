using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VividImagination.Shared.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence<TContext>(
        this IServiceCollection services,
        string connectionName,
        IConfiguration configuration)
        where TContext : DbContext
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<TContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString(connectionName),
                dbOptions =>
                {
                    dbOptions.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name);
                    dbOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                });

            // Suppress warnings for pending model changes
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        
        return services;
    }
}