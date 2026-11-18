namespace VividImagination.Shared.Infrastructure.Configurations;

public class GeneralSettings
{
    public string EncryptionKey { get; set; } = string.Empty;
    public string PublicBucket { get; set; } = string.Empty;
    public string DefaultClientUrl { get; set; } = string.Empty;
    public string DefaultLogoUrl { get; set; } = string.Empty;
    public string DefaultActivationMode { get; set; } = string.Empty;
    public string StorageType { get; set; } = default!;
    
    public string AppName { get; set; } = default!;
    public string DefaultTimeZoneId { get; set; } = default!;
    public bool AllowUsersToSetTimeZone { get; set; }

    //  Cryptography / secrets
    public string SecretKey { get; set; } = default!;
    public string DefaultAuthProvider { get; set; }= default!;
    public int MaxFileSizeInMb { get; set; } = default!;
    public List<string>? AllowedCountries { get; set; }
    
    public string MessageBrokerType { get; set; } = default!;
}