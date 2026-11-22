using System.Text.Json.Serialization;
using VividImagination.Mobile.OpenApi;

namespace VividImagination.Mobile.Models;

public sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

public sealed class UserProfile
{
    [JsonPropertyName("sub")]
    public string? Sub { get; set; }

    [JsonPropertyName("preferredUsername")]
    public string? PreferredUsername { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("safeBrowsingMode")]
    public SafeBrowsingMode SafeBrowsingMode { get; set; } = SafeBrowsingMode.BlurSensitive;

    [JsonPropertyName("isModerator")]
    public bool IsModerator { get; set; }

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(FullName)
            ? FullName
            : (!string.IsNullOrWhiteSpace(PreferredUsername) ? PreferredUsername : (Email ?? "Anonymous"));

    public static SafeBrowsingMode ParseSafeBrowsingMode(string? modeStr)
    {
        if (string.IsNullOrWhiteSpace(modeStr))
            return SafeBrowsingMode.BlurSensitive;

        if (Enum.TryParse<SafeBrowsingMode>(modeStr, true, out var mode))
            return mode;

        if (int.TryParse(modeStr, out var intVal) && Enum.IsDefined(typeof(SafeBrowsingMode), intVal))
            return (SafeBrowsingMode)intVal;

        return SafeBrowsingMode.BlurSensitive;
    }

    public static UserProfile FromDto(Profile profile)
    {
        return new UserProfile
        {
            Sub = profile.Sub,
            PreferredUsername = profile.PreferredUsername,
            Email = profile.Email,
            FullName = profile.FullName,
            SafeBrowsingMode = ParseSafeBrowsingMode(profile.SafeBrowsingMode),
            IsModerator = profile.Role?.Equals("Moderator", StringComparison.OrdinalIgnoreCase) == true ||
                          profile.PreferredUsername?.Contains("mod", StringComparison.OrdinalIgnoreCase) == true
        };
    }
}

public enum RegisterStatus
{
    Success,
    CreatedLoginRequired,
    EmailAlreadyExists,
    Failed
}

public sealed record RegisterResult(RegisterStatus Status, string? ErrorMessage = null, UserProfile? Profile = null);
