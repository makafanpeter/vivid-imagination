using System.Net.Http.Json;
using System.Text.Json;
using VividImagination.Mobile.Models;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.Services;

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

    public Task<RegisterResult> RegisterAsync(string email, string password, string? fullName, int safeBrowsingMode, CancellationToken ct = default)
    {
        var mode = Enum.IsDefined(typeof(SafeBrowsingMode), safeBrowsingMode)
            ? (SafeBrowsingMode)safeBrowsingMode
            : SafeBrowsingMode.BlurSensitive;
        return RegisterAsync(email, password, fullName, mode, ct);
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string? fullName = null, SafeBrowsingMode safeBrowsingMode = SafeBrowsingMode.BlurSensitive, CancellationToken ct = default)
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
            SafeBrowsingMode = safeBrowsingMode.ToString()
        };

        RegisterUserResult? result;
        try
        {
            result = await apiClient.RegisterUserAsync(request, ct);
        }
        catch (ApiException apiEx)
        {
            var resp = apiEx.Response ?? string.Empty;
            if (apiEx.StatusCode == 400 && resp.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return new RegisterResult(RegisterStatus.EmailAlreadyExists, "An account with this email address already exists.");
            }

            var errorMsg = ExtractErrorMessage(resp) ?? apiEx.Message;
            return new RegisterResult(RegisterStatus.Failed, errorMsg);
        }
        catch (Exception ex)
        {
            return new RegisterResult(RegisterStatus.Failed, ex.Message);
        }

        if (result == null || result.UserId <= 0)
        {
            return new RegisterResult(RegisterStatus.Failed, "Failed to create user account.");
        }

        // Account was created successfully! Now attempt auto-login
        try
        {
            var loggedIn = await LoginAsync(email, password, ct);
            if (loggedIn && CurrentUser != null)
            {
                return new RegisterResult(RegisterStatus.Success, Profile: CurrentUser);
            }
        }
        catch
        {
            // Login failed or was interrupted, but account was successfully created!
        }

        return new RegisterResult(RegisterStatus.CreatedLoginRequired);
    }

    private static string? ExtractErrorMessage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString();
            }
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
            if (doc.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }
        }
        catch
        {
        }
        return null;
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

            try
            {
                var prefs = await apiClient.GetUserPreferencesAsync(ct);
                if (prefs != null)
                {
                    if (prefs.IsPersonalisationEnabled.HasValue)
                    {
                        CurrentUser.IsPersonalisationEnabled = prefs.IsPersonalisationEnabled.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(prefs.SafeBrowsingMode))
                    {
                        CurrentUser.SafeBrowsingMode = UserProfile.ParseSafeBrowsingMode(prefs.SafeBrowsingMode);
                    }
                }
            }
            catch
            {
                // Non-fatal if preferences endpoint temporarily unavailable
            }

            AuthStateChanged?.Invoke(this, CurrentUser);
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
