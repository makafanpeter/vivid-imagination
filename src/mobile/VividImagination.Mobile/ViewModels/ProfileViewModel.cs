using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IConfigurationService _configurationService;

    public ProfileViewModel(
        IAuthService authService,
        IConfigurationService configurationService)
    {
        _authService = authService;
        _configurationService = configurationService;

        _authService.AuthStateChanged += (s, e) => UpdateFromAuth();
        _configurationService.EnvironmentChanged += (s, e) => UpdateFromConfig();

        UpdateFromAuth();
        UpdateFromConfig();
    }

    [ObservableProperty]
    private UserProfile? _profile;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private int _safeBrowsingIndex = 0; // 0 = Blur, 1 = Hide, 2 = Always Show

    [ObservableProperty]
    private int _environmentIndex = 0; // 0 = Development, 1 = Staging, 2 = Production

    [ObservableProperty]
    private string _activeBaseUrl = string.Empty;

    [ObservableProperty]
    private string _customApiUrl = string.Empty;

    [ObservableProperty]
    private bool _isDevelopment;

    [ObservableProperty]
    private string? _statusMessage;

    public string DisplayName => Profile?.DisplayName ?? "Anonymous Guest";
    public string UserEmail => Profile?.Email ?? "Not signed in";

    private void UpdateFromAuth()
    {
        Profile = _authService.CurrentUser;
        IsAuthenticated = Profile != null;
        if (Profile != null)
        {
            SafeBrowsingIndex = (int)Profile.SafeBrowsingMode;
        }
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(UserEmail));
    }

    private void UpdateFromConfig()
    {
        EnvironmentIndex = (int)_configurationService.CurrentEnvironment;
        ActiveBaseUrl = _configurationService.ActiveBaseUrl;
        CustomApiUrl = _configurationService.CustomUrl ?? string.Empty;
        IsDevelopment = _configurationService.CurrentEnvironment == AppEnvironment.Development;
    }

    [RelayCommand]
    public async Task RefreshProfileAsync()
    {
        Profile = await _authService.GetCurrentUserProfileAsync();
        UpdateFromAuth();
    }

    [RelayCommand]
    public async Task OpenLoginAsync()
    {
        await Shell.Current.GoToAsync("login");
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        UpdateFromAuth();
        StatusMessage = "Signed out.";
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        // Update environment
        _configurationService.CurrentEnvironment = (AppEnvironment)EnvironmentIndex;
        _configurationService.CustomUrl = string.IsNullOrWhiteSpace(CustomApiUrl) ? null : CustomApiUrl;

        if (Profile != null)
        {
            Profile.SafeBrowsingMode = (SafeBrowsingMode)SafeBrowsingIndex;
        }

        UpdateFromConfig();
        StatusMessage = "Settings saved successfully.";
        await Shell.Current.DisplayAlertAsync("Settings", $"Configuration updated. Connected to: {ActiveBaseUrl}", "OK");
    }
}
