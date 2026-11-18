using Microsoft.EntityFrameworkCore;
using VividImagination.Shared.Infrastructure.Persistence;

namespace VividImagination.Modules.Identity.Persistence.Infrastructure;

public class IdentityDbContextFactory 
    : DesignTimeDbContextFactoryBase<IdentityDbContext>
{
    protected override string ConnectionStringName => "Identity";
    protected override IdentityDbContext CreateNewInstance(DbContextOptions<IdentityDbContext> options)
    {
        return new IdentityDbContext(options);
    }

   
}