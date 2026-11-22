using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.OpenApi;
using VividImagination.Mobile.Services;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IConfigurationService _configurationService;
    private readonly IVividImaginationClient _apiClient;

    public ProfileViewModel(
        IAuthService authService,
        IConfigurationService configurationService,
        IVividImaginationClient apiClient)
    {
        _authService = authService;
        _configurationService = configurationService;
        _apiClient = apiClient;

        _authService.AuthStateChanged += (s, e) =>
        {
            UpdateFromAuth();
            _ = LoadUploadsAsync();
        };
        _configurationService.EnvironmentChanged += (s, e) => UpdateFromConfig();

        MyUploads = [];
        UpdateFromAuth();
        UpdateFromConfig();
        _ = LoadUploadsAsync();
    }

    public ObservableCollection<UserUploadItemViewModel> MyUploads { get; }
    public ObservableCollection<UserUploadItemViewModel> UserUploads => MyUploads;

    public bool HasUploads => MyUploads.Count > 0;
    public bool HasUserUploads => HasUploads;

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
        NotifyUploadsChanged();
    }

    private void UpdateFromConfig()
    {
        EnvironmentIndex = (int)_configurationService.CurrentEnvironment;
        ActiveBaseUrl = _configurationService.ActiveBaseUrl;
    }

    private void NotifyUploadsChanged()
    {
        OnPropertyChanged(nameof(PhotoCountText));
        OnPropertyChanged(nameof(HasUploads));
        OnPropertyChanged(nameof(HasUserUploads));
        OnPropertyChanged(nameof(UserUploads));
    }

    [RelayCommand]
    public async Task LoadUploadsAsync()
    {
        if (Profile == null)
        {
            MyUploads.Clear();
            NotifyUploadsChanged();
            return;
        }

        try
        {
            var mediaList = await _apiClient.GetMyMediaAsync(1, 50);
            MyUploads.Clear();
            if (mediaList != null)
            {
                foreach (var media in mediaList)
                {
                    MyUploads.Add(new UserUploadItemViewModel
                    {
                        PhotoId = media.Id,
                        ImageUrl = media.Url ?? string.Empty,
                        Caption = media.Description ?? media.Name ?? string.Empty,
                        Status = (media.Public == true) ? "Live" : "Checking"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load user uploads: {ex.Message}");
        }
        finally
        {
            NotifyUploadsChanged();
        }
    }

    [RelayCommand]
    public async Task RefreshProfileAsync()
    {
        Profile = await _authService.GetCurrentUserProfileAsync();
        UpdateFromAuth();
        await LoadUploadsAsync();
    }

    [RelayCommand]
    public async Task SetSafeBrowsingMode(string modeStr)
    {
        if (int.TryParse(modeStr, out var mode))
        {
            SafeBrowsingIndex = mode;
            if (Profile != null)
            {
                Profile.SafeBrowsingMode = (SafeBrowsingMode)mode;
            }
            try
            {
                await _apiClient.UpdateUserPreferencesAsync(mode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to persist safe browsing preference: {ex.Message}");
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
            try
            {
                await _apiClient.DeletePhotoAsync(item.PhotoId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete photo from API: {ex.Message}");
            }

            MyUploads.Remove(item);
            NotifyUploadsChanged();
            ToastService.Current.Show("Photo deleted", ToastType.Info);
        }
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        UpdateFromAuth();
        MyUploads.Clear();
        NotifyUploadsChanged();
        ToastService.Current.Show("Signed out", ToastType.Info);
        await Shell.Current.GoToAsync("login");
    }

    [RelayCommand]
    public async Task NavigateToUploadAsync()
    {
        await Shell.Current.GoToAsync("//uploadpage");
    }

    [RelayCommand]
    public async Task OpenLoginAsync()
    {
        await Shell.Current.GoToAsync("login");
    }
}
