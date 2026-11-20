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
    }
}
