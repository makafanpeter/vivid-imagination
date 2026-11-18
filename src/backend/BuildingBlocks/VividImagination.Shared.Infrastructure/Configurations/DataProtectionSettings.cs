namespace VividImagination.Shared.Infrastructure.Configurations;

public class DataProtectionSettings
{
    public string Storage { get; set; } = nameof(DataProtectionStorage.Local);

    public string? LocalKeysPath { get; set; }

    public string? AwsRegion { get; set; }

    public bool UseInstanceProfile { get; set; }

    public enum DataProtectionStorage
    {
        Local,
        AWSSSM
    }
}

