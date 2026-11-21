using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Feed.Persistence.Entities;

public class UserInteraction : BaseEntity<Guid>, IAggregate<Guid>
{
    [SetsRequiredMembers]
    public UserInteraction()
    {
        Id = Guid.NewGuid();
    }

    [BsonRepresentation(BsonType.String)]
    public Guid EventId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid PhotoId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid CreatorId { get; set; }

    public string InteractionType { get; set; } = string.Empty;
    public int FeedPosition { get; set; }
    public int DwellDurationMs { get; set; }
    public List<string> Tags { get; set; } = new();
    
}
