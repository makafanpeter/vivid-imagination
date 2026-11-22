using VividImagination.Mobile.Pages;
using VividImagination.Mobile.Services;

namespace VividImagination.Mobile;

public partial class AppShell : Shell
{
    private readonly IAuthService? _authService;

    public AppShell(IAuthService? authService = null)
    {
        InitializeComponent();
        _authService = authService;

        Routing.RegisterRoute("login", typeof(LoginPage));

        if (_authService != null)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (await _authService.IsAuthenticatedAsync())
                    {
                        await _authService.GetCurrentUserProfileAsync();
                    }
                }
                catch
                {
                    // Ignore startup connectivity or token expiration errors
                }
            });
        }
    }
}
