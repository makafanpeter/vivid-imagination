namespace VividImagination.Shared.Infrastructure.Configurations;

public class ApplicationSettings
{
    /// <summary>
    /// General application settings
    /// </summary>
    public GeneralSettings General { get; set; } = new();
    
    /// <summary>
    /// Cache-related settings
    /// </summary>
    public CacheSettings Cache { get; set; } = new();
    
    /// <summary>
    /// HTTP-related settings
    /// </summary>
    public HttpSettings Http { get; set; } = new();
    
    /// <summary>
    /// Image processing settings
    /// </summary>
    public ImageSettings Images { get; set; } = new();

    /// <summary>
    /// Cross-Origin Resource Sharing (CORS) settings.
    /// </summary>
    public CORS CORS { get; set; }

    /// <summary>
    /// Configuration settings related to storage operations and functionality.
    /// </summary>
    public StorageSettings Storage { get; set; }
    
    /// <summary>
    /// Security-related settings.
    /// </summary>
    public SecuritySettings SecuritySettings { get; set; } = new();
    
    /// <summary>
    /// Auth settings
    /// </summary>
    public OpenIdConnectSettings OpenIdConnectSettings { get; set; } = new();
    

    /// <summary>
    /// Contains configuration settings for data protection, allowing customization of storage
    /// options and the path for local key persistence in secure environments.
    /// </summary>
    public DataProtectionSettings DataProtectionSettings { get; set; } = new();
    

    /// <summary>
    /// Represents the configuration settings for the event bus,
    /// which manage communication between different subsystems or services.
    /// </summary>
    public EventBusSettings EventBus { get; set; } = new();

    /// <summary>
    /// Configuration for JSON Web Token (JWT) settings, including access token and refresh token lifetimes.
    /// </summary>
    public JwtSettings Jwt { get; set; } = new();
    
    
}
