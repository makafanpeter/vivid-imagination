using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Social.Persistence.Entities;

public class PhotoReport: BaseEntity<Guid>
{
    public Guid PhotoId { get; set; }
    public long ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
}