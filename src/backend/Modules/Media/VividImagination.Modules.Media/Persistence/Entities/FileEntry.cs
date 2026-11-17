using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Media.Persistence.Entities;

public class FileVariantInfo
{
    public required string VariantName { get; set; }
    public required string StorageKey { get; set; }
    public string? Url { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long Size { get; set; }
    public string ContentType { get; set; } = "image/webp";
}

public class FileEntry : BaseEntity<Guid>, IAggregate<Guid>, IFileEntry
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public long Size { get; set; }

    public DateTime UploadedTime { get; set; }

    public required string FileName { get; set; }

    public string? FileLocation { get; set; }
    public string? ContainerName { get; set; }
    public required string ContentType { get; set; }

    public string? OwnerId { get; set; } = string.Empty;
    public required string FileExtension { get; set; }

    public string? Url { get; set; }
    public string? SourceIp { get; set; }
    public bool Deleted { get; set; }

    public string Status { get; set; } = "Processing";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? BlurHash { get; set; }
    public Dictionary<string, FileVariantInfo> Variants { get; set; } = new();
}
