using System.Text.Json.Serialization;

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

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(FullName)
            ? FullName
            : (!string.IsNullOrWhiteSpace(PreferredUsername) ? PreferredUsername : (Email ?? "Anonymous"));
}

public sealed record VoteResult(
    [property: JsonPropertyName("photoId")] Guid PhotoId,
    [property: JsonPropertyName("upvotes")] int Upvotes,
    [property: JsonPropertyName("downvotes")] int Downvotes);

public sealed record ReportResult(
    [property: JsonPropertyName("photoId")] Guid PhotoId,
    [property: JsonPropertyName("totalReports")] int TotalReports);

public sealed record VoteRequest(
    [property: JsonPropertyName("value")] int Value);

public sealed record ReportRequest(
    [property: JsonPropertyName("reason")] string? Reason);
