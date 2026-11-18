using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VividImagination.Shared.Infrastructure.Mongo;

public class DbContextInitializer<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<DbContextInitializer<TContext>> logger)
    : IHostedService
    where TContext : IMongoDbContext
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();

        var contextName = typeof(TContext).Name;
        var specificConfig = configuration?.GetValue<bool?>($"MongoOptions:Contexts:{contextName}")
                          ?? configuration?.GetValue<bool?>($"MongoOptions:InitializeIndexes:{contextName}");
        var initializeIndexes = specificConfig
                             ?? configuration?.GetValue<bool?>("MongoOptions:InitializeIndexes")
                             ?? true;

        if (!initializeIndexes)
        {
            logger.LogInformation("Mongo index initialization disabled by configuration for {Context}. Skipping.", typeof(TContext).Name);
            return;
        }

        logger.LogInformation("Initializing {Context}...", typeof(TContext).Name);

        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        await context.InitializeAsync(ct);

        logger.LogInformation("{Context} initialized.", typeof(TContext).Name);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}