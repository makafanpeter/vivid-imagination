namespace VividImagination.Mobile.Services;

public interface ITokenStorageService
{
    Task SaveTokensAsync(string accessToken, string? refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearTokensAsync();
    Task<bool> HasTokenAsync();
}

public sealed class TokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    public async Task SaveTokensAsync(string accessToken, string? refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        else
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public Task ClearTokensAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }

    public async Task<bool> HasTokenAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}
