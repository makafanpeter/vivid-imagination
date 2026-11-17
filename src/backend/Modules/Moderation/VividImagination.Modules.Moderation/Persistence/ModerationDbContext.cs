using Humanizer;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Moderation.Persistence;

public class ModerationDbContext : MongoDbContext
{
    public ModerationDbContext(IOptions<MongoOptions> options) : base(options)
    {
        Reviews = GetCollection<Review>(nameof(Review).Pluralize());
    }

    public IMongoCollection<Review> Reviews { get; set; }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}