namespace VividImagination.Mobile.Services;

public static class AppConfig
{
    public static string BaseApiUrl
    {
        get
        {
            var customUrl = Preferences.Default.Get<string?>("custom_api_url", null);
            if (!string.IsNullOrWhiteSpace(customUrl))
            {
                return customUrl.TrimEnd('/');
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return "http://10.0.2.2:5101";
            }

            return "http://localhost:5101";
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Preferences.Default.Remove("custom_api_url");
            }
            else
            {
                Preferences.Default.Set("custom_api_url", value.TrimEnd('/'));
            }
        }
    }

    public static string FeedApiUrl => $"{BaseApiUrl}/api/v1/feed";
    public static string MediaApiUrl => $"{BaseApiUrl}/api/v1/media";
    public static string IdentityApiUrl => $"{BaseApiUrl}/api/v1/identity";
    public static string ConnectTokenUrl => $"{BaseApiUrl}/v1/connect/token";

    public static string GetPhotoVoteUrl(Guid photoId) => $"{BaseApiUrl}/api/v1/social-service/photos/{photoId}/vote";
    public static string GetPhotoReportUrl(Guid photoId) => $"{BaseApiUrl}/api/v1/social-service/photos/{photoId}/report";
    public static string GetDownloadUrl(Guid photoId) => $"{BaseApiUrl}/api/v1/media/{photoId}/download";
}
