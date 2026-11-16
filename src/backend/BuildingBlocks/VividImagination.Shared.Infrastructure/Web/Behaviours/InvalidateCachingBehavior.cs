using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Shared.Infrastructure.Web.Behaviours;

public class InvalidateCachingBehavior<TRequest, TResponse>(
    IStaticCacheManager cacheManager,
    ILogger<InvalidateCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IInvalidateCacheRequest invalidateCacheRequest)
        {
            // No cache invalidation request found, so just continue through the pipeline
            return await next(cancellationToken);
        }

        var response = await next(cancellationToken);

        if (invalidateCacheRequest.CacheKey != null)
        {
            var preparedKey = cacheManager.PrepareKey(invalidateCacheRequest.CacheKey, invalidateCacheRequest.CacheKeyParameters);
            await cacheManager.RemoveAsync(preparedKey);

            foreach (var prefix in preparedKey.Prefixes)
            {
                await cacheManager.RemoveByPrefixAsync(prefix);
            }

            logger.LogDebug("Cache data with cache key: {CacheKey} removed.", preparedKey.Key);
        }

        if (!string.IsNullOrEmpty(invalidateCacheRequest.Prefix))
        {
            await cacheManager.RemoveByPrefixAsync(invalidateCacheRequest.Prefix, invalidateCacheRequest.PrefixParameters);
            logger.LogDebug("Cache data with prefix: {Prefix} removed.", invalidateCacheRequest.Prefix);
        }

        foreach (var prefix in invalidateCacheRequest.Prefixes)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                await cacheManager.RemoveByPrefixAsync(prefix);
                logger.LogDebug("Cache data with prefix: {Prefix} removed.", prefix);
            }
        }

        return response;
    }
}