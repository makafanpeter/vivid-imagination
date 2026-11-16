namespace VividImagination.Shared.Infrastructure.Configurations;

public class CacheSettings
{
    public int ShortTermCacheTime { get; set; }
    public int DefaultCacheTime { get; set; }
    public bool EnableDistributedCache { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DistributedCache { get; set; } = string.Empty;
}