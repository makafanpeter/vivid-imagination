using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VividImagination.Modules.Social.Persistence.Entities;

namespace VividImagination.Modules.Social.Persistence.Infrastructure.Configurations;

public class PhotoVoteConfiguration : IEntityTypeConfiguration<PhotoVote>
{
    public void Configure(EntityTypeBuilder<PhotoVote> entity)
    {
        entity.ToTable("votes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.PhotoId).IsRequired();
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.Value).IsRequired();
        entity.Property(e => e.VotedAt).IsRequired();

        entity.HasIndex(e => new { e.PhotoId, e.UserId }).IsUnique();

       
    }
}
