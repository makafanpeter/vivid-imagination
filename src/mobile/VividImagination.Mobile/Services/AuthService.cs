using System.Net.Http.Json;
using System.Text.Json;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.Services;

public interface IAuthService
{
    UserProfile? CurrentUser { get; }
    Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<bool> RegisterAsync(string email, string password, string? fullName = null, int safeBrowsingMode = 0, CancellationToken ct = default);
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

    public void SetManualProfile(UserProfile profile)
    {
        CurrentUser = profile;
        AuthStateChanged?.Invoke(this, CurrentUser);
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var authConfig = configurationService.Options.Auth;
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password"),
                new("username", username),
                new("password", password),
                new("scope", authConfig.Scope),
                new("client_id", authConfig.ClientId)
            };

            if (!string.IsNullOrWhiteSpace(authConfig.ClientSecret))
            {
                parameters.Add(new("client_secret", authConfig.ClientSecret));
            }

            var content = new FormUrlEncodedContent(parameters);

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

    public async Task<bool> RegisterAsync(string email, string password, string? fullName = null, int safeBrowsingMode = 0, CancellationToken ct = default)
    {
        try
        {
            string? firstName = null;
            string? lastName = null;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = parts[0];
                if (parts.Length > 1)
                {
                    lastName = parts[1];
                }
            }

            var request = new RegisterUserRequest
            {
                Email = email.Trim().ToLowerInvariant(),
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                SafeBrowsingMode = safeBrowsingMode
            };

            var result = await apiClient.RegisterUserAsync(request, ct);
            if (result == null || result.UserId <= 0)
            {
                return false;
            }

            return await LoginAsync(email, password, ct);
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
