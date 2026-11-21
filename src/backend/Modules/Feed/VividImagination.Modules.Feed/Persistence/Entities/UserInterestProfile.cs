using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Feed.Persistence.Entities;

public class UserInterestProfile : BaseEntity<Guid>, IAggregate<Guid>
{
    [SetsRequiredMembers]
    public UserInterestProfile()
    {
        Id = Guid.NewGuid();
    }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    // Tag affinities: key is normalized lowercase tag (e.g. "landscape"), value is accumulated affinity score
    public Dictionary<string, double> TagAffinities { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Creator affinities: key is creator UserId as string, value is accumulated affinity score
    public Dictionary<string, double> CreatorAffinities { get; set; } = new();

    // Negative preferences
    public List<string> NegativeTags { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public List<Guid> NegativeCreators { get; set; } = new();

    // Interaction exclusions & fatigue
    [BsonRepresentation(BsonType.String)]
    public List<Guid> DownvotedPhotoIds { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public List<Guid> HiddenPhotoIds { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public List<Guid> RecentlyViewedPhotoIds { get; set; } = new();

    public int TotalInteractions { get; set; }
}
