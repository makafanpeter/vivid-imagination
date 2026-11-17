using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VividImagination.Mobile.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    public ProfileViewModel(IAuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += (s, e) => UpdateFromAuth();
        CustomApiUrl = AppConfig.BaseApiUrl;
        UpdateFromAuth();
    }

    [ObservableProperty]
    private UserProfile? _profile;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private int _safeBrowsingIndex = 0; // 0 = Blur, 1 = Hide, 2 = Always Show

    [ObservableProperty]
    private string _customApiUrl = string.Empty;

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
        if (!string.IsNullOrWhiteSpace(CustomApiUrl))
        {
            AppConfig.BaseApiUrl = CustomApiUrl;
        }

        if (Profile != null)
        {
            Profile.SafeBrowsingMode = (SafeBrowsingMode)SafeBrowsingIndex;
        }

        StatusMessage = "Settings saved successfully.";
        await Shell.Current.DisplayAlertAsync("Settings", "Preferences updated.", "OK");
    }
}
