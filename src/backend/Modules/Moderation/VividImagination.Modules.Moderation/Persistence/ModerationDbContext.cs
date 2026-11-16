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
        Reviews = GetCollection<ModerationReview>(nameof(ModerationReview).Pluralize());
    }

    public IMongoCollection<ModerationReview> Reviews { get; set; }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}