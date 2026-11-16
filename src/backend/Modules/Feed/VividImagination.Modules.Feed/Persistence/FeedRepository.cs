using MongoDB.Driver;
using VividImagination.Modules.Feed.Entities;

namespace VividImagination.Modules.Feed.Persistence;

public interface IFeedRepository
{
    Task<FeedItem?> GetByPhotoIdAsync(Guid photoId, CancellationToken ct = default);
    Task<List<FeedItem>> GetRankedFeedAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpsertFeedItemAsync(FeedItem item, CancellationToken ct = default);
    Task DeleteByPhotoIdAsync(Guid photoId, CancellationToken ct = default);
}

public class FeedRepository : IFeedRepository
{
    private readonly IMongoCollection<FeedItem> _collection;

    public FeedRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<FeedItem>("feed_items");
    }

    public async Task<FeedItem?> GetByPhotoIdAsync(Guid photoId, CancellationToken ct = default)
    {
        return await _collection.Find(f => f.PhotoId == photoId).FirstOrDefaultAsync(ct);
    }

    public async Task<List<FeedItem>> GetRankedFeedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var skip = Math.Max(0, (page - 1) * pageSize);
        return await _collection.Find(FilterDefinition<FeedItem>.Empty)
            .SortByDescending(f => f.Score)
            .ThenByDescending(f => f.UploadedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(ct);
    }

    public async Task UpsertFeedItemAsync(FeedItem item, CancellationToken ct = default)
    {
        var filter = Builders<FeedItem>.Filter.Eq(f => f.PhotoId, item.PhotoId);
        await _collection.ReplaceOneAsync(filter, item, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task DeleteByPhotoIdAsync(Guid photoId, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(f => f.PhotoId == photoId, ct);
    }
}
