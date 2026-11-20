using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.ViewModels;

public partial class LoginViewModel(IAuthService authService) : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    public async Task GoogleLoginAsync()
    {
        IsBusy = true;
        await Task.Delay(350);
        IsBusy = false;
        ToastService.Current.Show("Signed in with Google", ToastType.Success);
        await Shell.Current.GoToAsync("//feed");
    }

    [RelayCommand]
    public async Task AppleLoginAsync()
    {
        IsBusy = true;
        await Task.Delay(350);
        IsBusy = false;
        ToastService.Current.Show("Signed in with Apple", ToastType.Success);
        await Shell.Current.GoToAsync("//feed");
    }

    [RelayCommand]
    public async Task ModeratorLoginAsync()
    {
        IsBusy = true;
        await Task.Delay(350);
        IsBusy = false;

        // Sign in with Moderator role
        var modProfile = new UserProfile
        {
            Sub = "mod-1",
            PreferredUsername = "LeadModerator",
            FullName = "Vivid Lead Moderator",
            Email = "moderator@vividimagination.com",
            IsModerator = true,
            SafeBrowsingMode = SafeBrowsingMode.AlwaysShowSensitive
        };

        if (authService is AuthService concreteAuth)
        {
            concreteAuth.SetManualProfile(modProfile);
        }

        ToastService.Current.Show("Signed in as Moderator — Review queue unlocked!", ToastType.Success);
        await Shell.Current.GoToAsync("//feed");
    }

    [RelayCommand]
    public async Task BrowseAsGuestAsync()
    {
        await Shell.Current.GoToAsync("//feed");
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var success = await authService.LoginAsync(Username.Trim(), Password);
            if (success)
            {
                ToastService.Current.Show("Welcome back!", ToastType.Success);
                await Shell.Current.GoToAsync("//feed");
            }
            else
            {
                ErrorMessage = "Invalid credentials or service unavailable.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("//feed");
    }
}
