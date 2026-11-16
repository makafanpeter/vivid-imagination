using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VividImagination.Modules.Social.Persistence.Entities;

namespace VividImagination.Modules.Social.Persistence.Infrastructure.Configurations;

public class PhotoReportConfiguration : IEntityTypeConfiguration<PhotoReport>
{
    public void Configure(EntityTypeBuilder<PhotoReport> entity)
    {
        entity.ToTable("reports");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.PhotoId).IsRequired();
        entity.Property(e => e.ReporterUserId).IsRequired();
        entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        entity.Property(e => e.ReportedAt).IsRequired();

        // Prevent duplicate reports from same user on same photo
        entity.HasIndex(e => new { e.PhotoId, e.ReporterUserId }).IsUnique();
        
    }
}
