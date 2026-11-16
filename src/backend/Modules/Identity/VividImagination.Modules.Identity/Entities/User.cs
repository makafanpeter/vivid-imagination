using VividImagination.Modules.Identity.Contracts;

namespace VividImagination.Modules.Identity.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public SafeBrowsingMode SafeBrowsingMode { get; set; } = SafeBrowsingMode.BlurSensitive;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
