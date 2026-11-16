namespace VividImagination.Shared.Infrastructure.Common;

public static class AppConstants
{
    /// <summary>
    /// Gets the major store version
    /// </summary>
    public const string CurrentVersion = "0.00";

    /// <summary>
    /// Gets the minor store version
    /// </summary>
    public const string MinorVersion = "0";

    /// <summary>
    /// Gets the full store version
    /// </summary>
    public const string FullVersion = CurrentVersion + "." + MinorVersion;
    
    public const string DefaultApiVersion = "v1";

    public const string ApiRoute = "api/v{version:apiVersion}";

    public const string AppName = "vivid-imagination";
}