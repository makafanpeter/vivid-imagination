using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace VividImagination.Shared.Infrastructure.Caching
{
    [DisallowConcurrentExecution]
    public class ClearCacheJob(
        ILogger<ClearCacheJob> logger,
        IServiceProvider provider)
        : IJob
    {
        private async Task ClearCache()
        {
            using var scope = provider.CreateScope();
            try
            {
                var cacheManager = scope.ServiceProvider.GetRequiredService<IStaticCacheManager>();
                await cacheManager.ClearAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while clearing cache");
            }
        }




        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("{Type}  is starting",GetType().Name);
             await ClearCache();
            logger.LogInformation("{Type}  is completed",GetType().Name);
        }
    }
}
