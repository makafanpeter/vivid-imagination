using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Shared.Infrastructure.Caching;

namespace VividImagination.Shared.Infrastructure.Web.Behaviours;

public class CachingBehavior<TRequest, TResponse>(
    IStaticCacheManager cacheManager,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheRequest cacheRequest)
            // No cache request found, so just continue through the pipeline
            return await next(cancellationToken);
        
        var cacheKey = cacheRequest.CacheKey;
        var parameters = cacheRequest.CacheKeyParameters;
        
        var preparedKey = cacheManager.PrepareKey(cacheKey, parameters);
        
        logger.LogDebug("Response retrieved {TRequest} from cache. CacheKey: {CacheKey}",
            typeof(TRequest).FullName, preparedKey);
        
        return  await cacheManager.GetAsync(preparedKey, async () =>
        {
            logger.LogDebug("Caching response for {TRequest} with cache key: {CacheKey}", typeof(TRequest).FullName,
                preparedKey);
            
            var response = await next(cancellationToken);
           
            return response; 
        });

       
        
    }
}