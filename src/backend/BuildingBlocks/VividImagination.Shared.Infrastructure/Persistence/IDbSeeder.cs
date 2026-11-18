using Microsoft.EntityFrameworkCore;

namespace VividImagination.Shared.Infrastructure.Persistence;

public interface IDbSeeder<in TContext> where TContext : DbContext
{
    Task SeedAsync(TContext context);
}