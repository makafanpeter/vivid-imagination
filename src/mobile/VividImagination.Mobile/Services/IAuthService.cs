using VividImagination.Mobile.Models;

namespace VividImagination.Mobile.Services;

public interface IAuthService
{
    UserProfile? CurrentUser { get; }
    Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<RegisterResult> RegisterAsync(string email, string password, string? fullName = null, SafeBrowsingMode safeBrowsingMode = SafeBrowsingMode.BlurSensitive, CancellationToken ct = default);
    Task<RegisterResult> RegisterAsync(string email, string password, string? fullName, int safeBrowsingMode, CancellationToken ct = default);
    Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken ct = default);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    event EventHandler<UserProfile?>? AuthStateChanged;
}
