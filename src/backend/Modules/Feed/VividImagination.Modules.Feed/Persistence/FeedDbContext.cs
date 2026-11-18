using Humanizer;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Persistence;

public class FeedDbContext : MongoDbContext
{
    public FeedDbContext(IOptions<MongoOptions> options) : base(options)
    {
        FeedItems = GetCollection<FeedItem>(nameof(FeedItem).Pluralize());
    }

    public IMongoCollection<FeedItem> FeedItems { get; set; }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<FeedItem>(
                Builders<FeedItem>.IndexKeys
                    .Descending(x => x.Score)
                    .Descending(x => x.UploadedAt),
                new CreateIndexOptions { Name = "idx_score_desc_uploadedAt_desc" })
        };

        await FeedItems.Indexes.CreateManyAsync(indexes, ct);
    }
}
