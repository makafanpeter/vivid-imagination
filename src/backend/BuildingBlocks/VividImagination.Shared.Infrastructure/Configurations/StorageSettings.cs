namespace VividImagination.Shared.Infrastructure.Configurations;

public class S3StorageSettings
{
    public string BucketName { get; set; } = "vivid-imagination-photos";
    public string Region { get; set; } = "eu-west-1";
    public string? ServiceUrl { get; set; }
}

public class StorageSettings
{
    public LocalStorageSettings Local { get; set; } = new();
    public S3StorageSettings S3 { get; set; } = new();
}
