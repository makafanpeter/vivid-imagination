using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VividImagination.Shared.Infrastructure.Caching.Redis;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Caching;

public static class Extensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationSettings = configuration.GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new ArgumentNullException(nameof(ApplicationSettings));

        services.AddHttpContextAccessor();

        if (applicationSettings.Cache.EnableDistributedCache)
        {
            // ONE multiplexer for the whole process.
            services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

            // Scoped because of the per-request cache. Register the concrete type once and
            // forward the interfaces to it, so ILocker and IStaticCacheManager share one instance per scope.
            services.AddScoped<DistributedCacheManager>();
            services.AddScoped<IStaticCacheManager>(sp => sp.GetRequiredService<DistributedCacheManager>());
            services.AddScoped<ILocker>(sp => sp.GetRequiredService<DistributedCacheManager>());
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<MemoryCacheManager>();
            services.AddSingleton<IStaticCacheManager>(sp => sp.GetRequiredService<MemoryCacheManager>());
            services.AddSingleton<ILocker>(sp => sp.GetRequiredService<MemoryCacheManager>());
        }

        return services;
    }
}