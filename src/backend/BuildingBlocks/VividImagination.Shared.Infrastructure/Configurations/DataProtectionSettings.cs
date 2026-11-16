namespace VividImagination.Shared.Infrastructure.Configurations;

public class DataProtectionSettings
{
    public string Storage { get; set; }= string.Empty;
    public string LocalKeysPath { get; set; }= string.Empty;
    
    public enum DataProtectionStorage
    {
        Local = 0,
    }
}

