using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Media.Entities;

namespace VividImagination.Modules.Media.Persistence;

public class MediaDbContext : DbContext
{
    public const string Schema = "media";

    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("photos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BlurHash).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Caption).HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasIndex(e => e.UserId);
        });
    }
}
