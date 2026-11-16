namespace VividImagination.Modules.Media.Entities;

public class Photo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string BlurHash { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
