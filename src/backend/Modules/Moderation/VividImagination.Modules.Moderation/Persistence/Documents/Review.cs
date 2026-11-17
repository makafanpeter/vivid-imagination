using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Moderation.Persistence.Documents;

public class Review: BaseEntity<Guid>, IAggregate<Guid>
{

    [BsonRepresentation(BsonType.String)]
    public Guid PhotoId { get; set; }

    public double MaxRiskScore { get; set; }
    public double AdultScore { get; set; }
    public double ViolenceScore { get; set; }
    public double HateScore { get; set; }

    public string? SafetyCategory { get; set; }
    public string Verdict { get; set; } = "Approved"; // Approved, TaggedUnsafe, Rejected
    public bool IsUnsafe { get; set; }
    public bool BlurRequired { get; set; }

    public int ReportCount { get; set; }

    public List<RekognitionLabelInfo> Labels { get; set; } = [];

    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}

public class RekognitionLabelInfo
{
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public float Confidence { get; set; }
}
