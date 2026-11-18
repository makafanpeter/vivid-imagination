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
        Routing.RegisterRoute("review", typeof(ReviewQueuePage));

        if (_authService != null)
        {
            _authService.AuthStateChanged += (s, profile) =>
            {
                UpdateReviewTab(profile?.IsModerator == true);
            };
            UpdateReviewTab(_authService.CurrentUser?.IsModerator == true);
        }
        else
        {
            UpdateReviewTab(false);
        }
    }

    private void UpdateReviewTab(bool isModerator)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ReviewTab != null)
            {
                ReviewTab.IsVisible = isModerator;
            }
        });
    }
}
