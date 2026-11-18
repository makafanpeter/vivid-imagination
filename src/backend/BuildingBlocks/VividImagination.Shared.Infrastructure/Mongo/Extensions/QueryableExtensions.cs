using MongoDB.Driver;
using MongoDB.Driver.Linq;
using VividImagination.Shared.Domain.Entities;
using VividImagination.Shared.Domain.Infrastructure;

namespace VividImagination.Shared.Infrastructure.Mongo.Extensions;

public static class QueryableExtensions
{
    
    // Overload for IFindFluent if you prefer the MongoDB driver's native query API
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IFindFluent<T, T> source, 
        int pageIndex, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountDocumentsAsync(cancellationToken);
        
        var items = await source
            .Skip(pageIndex * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedList<T>(items,  pageIndex, pageSize, (int)count);
    }
    
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source, 
        int pageIndex, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
            
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative");
            
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive");

        // Execute count query
        var count = await source.CountAsync(cancellationToken);
        
        // No need to query the database if there are no results
        if (count == 0)
            return new PagedList<T>(Array.Empty<T>(), pageIndex, pageSize, count);
        
        // Execute the main query with pagination
        var items = await source
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedList<T>(items, pageIndex, pageSize, count);
    }
}