using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Services;
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

        MyUploads = [];
        UpdateFromAuth();
        UpdateFromConfig();
    }

    public ObservableCollection<UserUploadItemViewModel> MyUploads { get; }

    [ObservableProperty]
    private UserProfile? _profile;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBlurSelected))]
    [NotifyPropertyChangedFor(nameof(IsHideSelected))]
    [NotifyPropertyChangedFor(nameof(IsAlwaysShowSelected))]
    private int _safeBrowsingIndex = 0; // 0 = Blur, 1 = Hide, 2 = Always Show

    public bool IsBlurSelected => SafeBrowsingIndex == 0;
    public bool IsHideSelected => SafeBrowsingIndex == 1;
    public bool IsAlwaysShowSelected => SafeBrowsingIndex == 2;

    [ObservableProperty]
    private int _environmentIndex = 0;

    [ObservableProperty]
    private string _activeBaseUrl = string.Empty;

    public string DisplayName => Profile?.DisplayName ?? "Creator";
    public string PhotoCountText => $"{MyUploads.Count} photos";

    private void UpdateFromAuth()
    {
        Profile = _authService.CurrentUser;
        IsAuthenticated = Profile != null;
        if (Profile != null)
        {
            SafeBrowsingIndex = (int)Profile.SafeBrowsingMode;
        }
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(PhotoCountText));
    }

    private void UpdateFromConfig()
    {
        EnvironmentIndex = (int)_configurationService.CurrentEnvironment;
        ActiveBaseUrl = _configurationService.ActiveBaseUrl;
    }


    [RelayCommand]
    public async Task RefreshProfileAsync()
    {
        Profile = await _authService.GetCurrentUserProfileAsync();
        UpdateFromAuth();
    }

    [RelayCommand]
    public void SetSafeBrowsingMode(string modeStr)
    {
        if (int.TryParse(modeStr, out var mode))
        {
            SafeBrowsingIndex = mode;
            if (Profile != null)
            {
                Profile.SafeBrowsingMode = (SafeBrowsingMode)mode;
            }
            ToastService.Current.Show($"Browsing mode set to {(SafeBrowsingMode)mode}", ToastType.Info);
        }
    }

    [RelayCommand]
    public async Task EditCaptionAsync(UserUploadItemViewModel? item)
    {
        if (item == null) return;

        var newCaption = await Shell.Current.DisplayPromptAsync(
            "Rewrite Caption",
            "Update the caption for this photo:",
            initialValue: item.Caption,
            maxLength: 500,
            keyboard: Keyboard.Text);

        if (!string.IsNullOrWhiteSpace(newCaption) && newCaption != item.Caption)
        {
            item.Caption = newCaption;
            ToastService.Current.Show("Caption updated!", ToastType.Success);
        }
    }

    [RelayCommand]
    public async Task DeleteUploadAsync(UserUploadItemViewModel? item)
    {
        if (item == null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Photo",
            "Are you sure you want to delete this photo from your uploads?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            MyUploads.Remove(item);
            OnPropertyChanged(nameof(PhotoCountText));
            ToastService.Current.Show("Photo deleted", ToastType.Info);
        }
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        UpdateFromAuth();
        ToastService.Current.Show("Signed out", ToastType.Info);
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    public async Task OpenLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }
}
