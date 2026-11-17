using Microsoft.Extensions.Options;
using VividImagination.Mobile.Models;

namespace VividImagination.Mobile.Services.Configuration;

public interface IConfigurationService
{
    AppEnvironment CurrentEnvironment { get; set; }
    string ActiveBaseUrl { get; }
    string? CustomUrl { get; set; }
    AppConfigOptions Options { get; }
    string GetDownloadUrl(Guid photoId);
    string GetTokenUrl();
    event EventHandler<AppEnvironment>? EnvironmentChanged;
}

public sealed class ConfigurationService : IConfigurationService
{
    private const string EnvironmentPrefKey = "vivid_app_environment";
    private const string CustomUrlPrefKey = "vivid_custom_api_url";

    private readonly IOptionsMonitor<AppConfigOptions> _optionsMonitor;
    private AppEnvironment _currentEnvironment;
    private string? _customUrl;

    public ConfigurationService(IOptionsMonitor<AppConfigOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;

        // Restore persisted environment preference or fallback to configuration
        var savedEnv = Preferences.Default.Get<int?>(EnvironmentPrefKey, null);
        if (savedEnv.HasValue && Enum.IsDefined(typeof(AppEnvironment), savedEnv.Value))
        {
            _currentEnvironment = (AppEnvironment)savedEnv.Value;
        }
        else
        {
            _currentEnvironment = _optionsMonitor.CurrentValue.Environment;
        }

        _customUrl = Preferences.Default.Get<string?>(CustomUrlPrefKey, null);
    }

    public AppConfigOptions Options => _optionsMonitor.CurrentValue;

    public AppEnvironment CurrentEnvironment
    {
        get => _currentEnvironment;
        set
        {
            if (_currentEnvironment != value)
            {
                _currentEnvironment = value;
                Preferences.Default.Set(EnvironmentPrefKey, (int)value);
                EnvironmentChanged?.Invoke(this, value);
            }
        }
    }

    public string? CustomUrl
    {
        get => _customUrl;
        set
        {
            _customUrl = string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/');
            if (_customUrl == null)
            {
                Preferences.Default.Remove(CustomUrlPrefKey);
            }
            else
            {
                Preferences.Default.Set(CustomUrlPrefKey, _customUrl);
            }
            EnvironmentChanged?.Invoke(this, _currentEnvironment);
        }
    }

    public string ActiveBaseUrl
    {
        get
        {
            if (CurrentEnvironment == AppEnvironment.Development && !string.IsNullOrWhiteSpace(_customUrl))
            {
                return _customUrl;
            }

            return CurrentEnvironment switch
            {
                AppEnvironment.Production => Options.Api.DesktopBaseUrl.StartsWith("https://api")
                    ? Options.Api.DesktopBaseUrl
                    : "https://api.vividimagination.com",
                AppEnvironment.Staging => "https://staging-api.vividimagination.com",
                _ => Options.Api.ResolveDefaultBaseUrl(DeviceInfo.Platform)
            };
        }
    }

    public string GetDownloadUrl(Guid photoId) => $"{ActiveBaseUrl}/api/v1/media/{photoId}/download";

    public string GetTokenUrl() => $"{ActiveBaseUrl}/v1/connect/token";

    public event EventHandler<AppEnvironment>? EnvironmentChanged;
}
