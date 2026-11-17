using Humanizer;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Persistence;

public class FeedDbContext: MongoDbContext
{
    public FeedDbContext(IOptions<MongoOptions> options) : base(options)
    {
        FeedItems = GetCollection<FeedItem>(nameof(FeedItem).Pluralize());
    }
    
    
    public IMongoCollection<FeedItem> FeedItems { get; set; }


    public override async Task InitializeAsync(CancellationToken ct = default)
    {
       
    }
}