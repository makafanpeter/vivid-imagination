using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Social.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Persistence;

namespace VividImagination.Modules.Social.Persistence;

public class SocialDbContext(DbContextOptions<SocialDbContext> options) : AppDbContextBase(options)
{
    private const string Schema = "social";

    public DbSet<PhotoVote> Votes => Set<PhotoVote>();
    public DbSet<PhotoReport> Reports => Set<PhotoReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }
}
