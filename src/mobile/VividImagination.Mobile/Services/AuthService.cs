using System.Net.Http.Json;
using System.Text.Json;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.Services;

public interface IAuthService
{
    UserProfile? CurrentUser { get; }
    Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken ct = default);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    event EventHandler<UserProfile?>? AuthStateChanged;
}

public sealed class AuthService(
    HttpClient httpClient,
    ITokenStorageService tokenStorage,
    IConfigurationService configurationService,
    IVividImaginationClient apiClient) : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserProfile? CurrentUser { get; private set; }
    public event EventHandler<UserProfile?>? AuthStateChanged;

    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("scope", "offline_access openid profile")
            ]);

            var tokenUrl = configurationService.GetTokenUrl();
            var response = await httpClient.PostAsync(tokenUrl, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct);
            if (tokenResponse?.AccessToken is null)
            {
                return false;
            }

            await tokenStorage.SaveTokensAsync(tokenResponse.AccessToken, tokenResponse.RefreshToken);
            CurrentUser = await GetCurrentUserProfileAsync(ct);
            AuthStateChanged?.Invoke(this, CurrentUser);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var hasToken = await tokenStorage.HasTokenAsync();
            if (!hasToken)
            {
                CurrentUser = null;
                return null;
            }

            var profileDto = await apiClient.GetCurrentUserAsync(ct);
            if (profileDto == null)
            {
                return null;
            }

            CurrentUser = UserProfile.FromDto(profileDto);
            return CurrentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await tokenStorage.ClearTokensAsync();
        CurrentUser = null;
        AuthStateChanged?.Invoke(this, null);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await tokenStorage.HasTokenAsync();
    }
}
