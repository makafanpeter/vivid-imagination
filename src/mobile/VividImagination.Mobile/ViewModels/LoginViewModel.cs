using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.ViewModels;

public partial class LoginViewModel(IAuthService authService) : ObservableObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [ObservableProperty]
    private string _usernameOrEmail = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _signInButtonText = "Sign In";

    [ObservableProperty]
    private string _registerButtonText = "Create Account";

    [ObservableProperty]
    private string? _errorMessage;

    private int _isSubmitting;

    [RelayCommand]
    public void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = null;
    }

    [RelayCommand]
    public void SetSignInMode()
    {
        IsRegisterMode = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    public void SetRegisterMode()
    {
        IsRegisterMode = true;
        ErrorMessage = null;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task LoginAsync()
    {
        if (Interlocked.CompareExchange(ref _isSubmitting, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(UsernameOrEmail) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username/email and password.";
                return;
            }

            IsBusy = true;
            SignInButtonText = "Signing in...";
            ErrorMessage = null;

            var success = await authService.LoginAsync(UsernameOrEmail.Trim(), Password);
            if (success)
            {
                ToastService.Current.Show("Welcome back!", ToastType.Success);
                await Shell.Current.GoToAsync("//feedpage");
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
            SignInButtonText = "Sign In";
            IsBusy = false;
            Interlocked.Exchange(ref _isSubmitting, 0);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task RegisterAsync()
    {
        if (Interlocked.CompareExchange(ref _isSubmitting, 1, 0) != 0)
        {
            return;
        }

        try
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Please enter your full name.";
                return;
            }

            var email = UsernameOrEmail.Trim();
            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            if (string.IsNullOrEmpty(Password) || Password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters long.";
                return;
            }

            if (!Password.Any(char.IsUpper))
            {
                ErrorMessage = "Password must contain at least one uppercase letter.";
                return;
            }

            if (!Password.Any(char.IsLower))
            {
                ErrorMessage = "Password must contain at least one lowercase letter.";
                return;
            }

            if (!Password.Any(char.IsDigit))
            {
                ErrorMessage = "Password must contain at least one number.";
                return;
            }

            if (!Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                ErrorMessage = "Password must contain at least one special character.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            IsBusy = true;
            RegisterButtonText = "Creating account...";

            var result = await authService.RegisterAsync(email, Password, FullName.Trim());
            switch (result.Status)
            {
                case RegisterStatus.Success:
                    ToastService.Current.Show("Account created! Welcome to Vivid.", ToastType.Success);
                    await Shell.Current.GoToAsync("//feedpage");
                    break;

                case RegisterStatus.CreatedLoginRequired:
                    IsRegisterMode = false;
                    ErrorMessage = null;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;
                    ToastService.Current.Show("Account created! Please enter your password to sign in.", ToastType.Success);
                    break;

                case RegisterStatus.EmailAlreadyExists:
                    ErrorMessage = "An account with this email already exists. Please switch to Sign In.";
                    break;

                case RegisterStatus.Failed:
                default:
                    ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? result.ErrorMessage
                        : "Registration failed. Please try again.";
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration failed: {ex.Message}";
        }
        finally
        {
            RegisterButtonText = "Create Account";
            IsBusy = false;
            Interlocked.Exchange(ref _isSubmitting, 0);
        }
    }

    [RelayCommand]
    public async Task BrowseAsGuestAsync()
    {
        await Shell.Current.GoToAsync("//feedpage");
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("//feedpage");
    }
}
