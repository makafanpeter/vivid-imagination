using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.Services;

public static class AppConfig
{
    public static IConfigurationService? Service { get; set; }

    public static string BaseApiUrl => Service?.ActiveBaseUrl ?? (DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5101" : "http://localhost:5101");
    public static string FeedApiUrl => $"{BaseApiUrl}/api/v1/feed";
    public static string MediaApiUrl => $"{BaseApiUrl}/api/v1/media";
    public static string IdentityApiUrl => $"{BaseApiUrl}/api/v1/identity";
    public static string ConnectTokenUrl => Service?.GetTokenUrl() ?? $"{BaseApiUrl}/v1/connect/token";
    public static string AuthClientId => Service?.Options.Auth.ClientId ?? "VividImagination-client";
    public static string? AuthClientSecret => Service?.Options.Auth.ClientSecret;

    public static string GetPhotoVoteUrl(Guid photoId) => $"{BaseApiUrl}/api/v1/social-service/photos/{photoId}/vote";
    public static string GetPhotoReportUrl(Guid photoId) => $"{BaseApiUrl}/api/v1/social-service/photos/{photoId}/report";
    public static string GetDownloadUrl(Guid photoId) => Service?.GetDownloadUrl(photoId) ?? $"{BaseApiUrl}/api/v1/media/{photoId}/download";
}
