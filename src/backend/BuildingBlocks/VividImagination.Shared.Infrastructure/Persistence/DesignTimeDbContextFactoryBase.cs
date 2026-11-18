using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VividImagination.Shared.Infrastructure.Persistence;

/// <summary>
/// Base class for design-time DbContext factory implementations
/// </summary>
public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    
    private const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
    private const string DefaultSettingsFile = "appsettings.json";
    private const string DevelopmentSettingsFile = "appsettings.Development.json";
    private const string EnvironmentSettingsFormat = "appsettings.{0}.json";
    protected virtual string ConnectionStringName => "ConnectionString";

    /// <summary>
    /// Creates a new DbContext instance
    /// </summary>
    public TContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable(AspNetCoreEnvironment);
        var basePath = AppContext.BaseDirectory;
        return CreateDbContextInstance(basePath, environmentName);
    }

    /// <summary>
    /// Creates a new instance of the DbContext
    /// </summary>
    protected abstract TContext CreateNewInstance(DbContextOptions<TContext> options);

    private TContext CreateDbContextInstance(string basePath, string environmentName)
    {
        if (string.IsNullOrEmpty(basePath))
            throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

        var configuration = BuildConfiguration(basePath, environmentName);
        var connectionString = GetConnectionString(configuration);
        var options = CreateDbContextOptions(connectionString);

        return CreateNewInstance(options);
    }

    private IConfiguration BuildConfiguration(string basePath, string environmentName)
    {
        Console.WriteLine($"[DesignTime] BasePath: {basePath}");
        Console.WriteLine($"[DesignTime] Environment: {environmentName}");
        Console.WriteLine($"[DesignTime] Looking for appsettings in: {basePath}");
        Console.WriteLine($"[DesignTime] Files found: {string.Join(", ", Directory.GetFiles(basePath, "appsettings*"))}");
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(DefaultSettingsFile)
            .AddJsonFile(string.Format(EnvironmentSettingsFormat, environmentName), optional: true)
            .AddJsonFile(DevelopmentSettingsFile)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    private string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                $"Connection string '{ConnectionStringName}' is null or empty.",
                nameof(connectionString));
        }

        return connectionString;
    }

    private DbContextOptions<TContext> CreateDbContextOptions(string connectionString)
    {
        Console.WriteLine($"Creating DbContext with connection string: '{connectionString}'.");

        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString)
            .Options;
    }
}