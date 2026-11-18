using Microsoft.EntityFrameworkCore;
using VividImagination.Shared.Infrastructure.Persistence;

namespace VividImagination.Modules.Social.Persistence.Infrastructure;

public class SocialDbContextFactory : DesignTimeDbContextFactoryBase<SocialDbContext>
{
    protected override string ConnectionStringName => "Social"; 
    protected override SocialDbContext CreateNewInstance(DbContextOptions<SocialDbContext> options)
        => new(options);
}