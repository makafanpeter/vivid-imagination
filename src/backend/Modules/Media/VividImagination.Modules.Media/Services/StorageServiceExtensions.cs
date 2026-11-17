using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VividImagination.Modules.Media.Persistence.Enums;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Modules.Media.Services.FileService.LocalFiles;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Modules.Media.Services;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get application settings
        var applicationSettings = configuration
            .GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() 
            ?? throw new InvalidOperationException(
                $"{nameof(ApplicationSettings)} section is missing in configuration");

        // Parse storage type
        if (!Enum.TryParse<StorageType>(
            applicationSettings.General.StorageType, 
            ignoreCase: true, 
            out var storageType))
        {
            throw new InvalidOperationException(
                $"Invalid storage type: {applicationSettings.General.StorageType}. " +
                $"Valid values are: {string.Join(", ", Enum.GetNames<StorageType>())}");
        }
        // Register appropriate file service
        switch (storageType)
        {
            case StorageType.Local:
                services.AddScoped<IFileServiceManager, LocalFileServiceManager>();
                Log.Information("Configured Local file storage");
                break;

            case StorageType.S3:
                //services.AddScoped<IFileServiceManager, S3FileServiceManager>();
                //Log.Information("Configured S3 file storage");
                //break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(storageType), 
                    storageType, 
                    "Unsupported storage type");
        }

        return services;
    }
}