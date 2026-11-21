using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Feed.Persistence.Entities;

public class FeedItem: BaseEntity<Guid>, IAggregate<Guid>
{
    

    [BsonRepresentation(BsonType.String)]
    public Guid PhotoId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public string StorageKey { get; set; } = string.Empty;
    public string BlurHash { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;

    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public double Score { get; set; }

    public bool IsUnsafe { get; set; }
    public bool BlurRequired { get; set; }
    public string? SafetyCategory { get; set; }
    public string Verdict { get; set; } = "Approved";
}
