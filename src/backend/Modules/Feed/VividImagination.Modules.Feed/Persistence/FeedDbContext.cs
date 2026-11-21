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
        UserInterestProfiles = GetCollection<UserInterestProfile>("user_interest_profiles");
        UserInteractions = GetCollection<UserInteraction>("user_interactions");
    }

    public IMongoCollection<FeedItem> FeedItems { get; set; }
    public IMongoCollection<UserInterestProfile> UserInterestProfiles { get; set; }
    public IMongoCollection<UserInteraction> UserInteractions { get; set; }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        var feedIndexes = new[]
        {
            new CreateIndexModel<FeedItem>(
                Builders<FeedItem>.IndexKeys
                    .Descending(x => x.Score)
                    .Descending(x => x.CreatedOn),
                new CreateIndexOptions { Name = "idx_score_desc_uploadedAt_desc" })
        };
        await FeedItems.Indexes.CreateManyAsync(feedIndexes, ct);

        try
        {
            var profileIndexes = new[]
            {
                new CreateIndexModel<UserInterestProfile>(
                    Builders<UserInterestProfile>.IndexKeys.Ascending(x => x.UserId),
                    new CreateIndexOptions { Name = "idx_user_interest_profiles_userId_unique", Unique = true })
            };
            await UserInterestProfiles.Indexes.CreateManyAsync(profileIndexes, ct);

            var interactionIndexes = new[]
            {
                new CreateIndexModel<UserInteraction>(
                    Builders<UserInteraction>.IndexKeys.Ascending(x => x.EventId),
                    new CreateIndexOptions { Name = "idx_user_interactions_eventId_unique", Unique = true }),
                new CreateIndexModel<UserInteraction>(
                    Builders<UserInteraction>.IndexKeys
                        .Ascending(x => x.UserId)
                        .Descending(x => x.CreatedOn),
                    new CreateIndexOptions { Name = "idx_user_interactions_userId_createdOn" })
            };
            await UserInteractions.Indexes.CreateManyAsync(interactionIndexes, ct);
        }
        catch (Exception)
        {
            // Ignored in unit testing environments where secondary collections are unmocked
        }
    }
}
