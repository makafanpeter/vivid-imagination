namespace VividImagination.Mobile.Models;

public enum AppEnvironment
{
    Development = 0,
    Staging = 1,
    Production = 2
}

public class AppConfigOptions
{
    public const string SectionName = "AppConfig";

    public AppEnvironment Environment { get; set; } = AppEnvironment.Development;
    public ApiEndpointsOptions Api { get; set; } = new();
    public SafetyOptions Safety { get; set; } = new();
}

public class ApiEndpointsOptions
{
    public string DesktopBaseUrl { get; set; } = "http://localhost:5101";
    public string AndroidEmulatorBaseUrl { get; set; } = "http://10.0.2.2:5101";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;

    public string ResolveDefaultBaseUrl(DevicePlatform platform)
    {
        return platform == DevicePlatform.Android ? AndroidEmulatorBaseUrl : DesktopBaseUrl;
    }
}

public class SafetyOptions
{
    public SafeBrowsingMode DefaultMode { get; set; } = SafeBrowsingMode.BlurSensitive;
    public bool EnableBlurHashPlaceholders { get; set; } = true;
}
