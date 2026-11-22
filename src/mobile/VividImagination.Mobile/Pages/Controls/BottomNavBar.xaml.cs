using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.Pages.Controls;

public partial class BottomNavBar : ContentView
{
    private static readonly Color ActiveColor = Color.FromArgb("#FF551C");
    private static readonly Color InactiveColor = Color.FromArgb("#BDB5AE");

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(
            nameof(ActiveTab),
            typeof(string),
            typeof(BottomNavBar),
            "Feed",
            propertyChanged: OnActiveTabChanged);

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public bool IsFeedActive => string.Equals(ActiveTab, "Feed", StringComparison.OrdinalIgnoreCase);
    public bool IsUploadActive => string.Equals(ActiveTab, "Upload", StringComparison.OrdinalIgnoreCase);
    public bool IsReviewActive => string.Equals(ActiveTab, "Review", StringComparison.OrdinalIgnoreCase);
    public bool IsMeActive => string.Equals(ActiveTab, "Me", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(ActiveTab, "Profile", StringComparison.OrdinalIgnoreCase);

    public Color FeedTextColor => IsFeedActive ? ActiveColor : InactiveColor;
    public Color ReviewTextColor => IsReviewActive ? ActiveColor : InactiveColor;
    public Color MeTextColor => IsMeActive ? ActiveColor : InactiveColor;

    private bool _isModerator;
    public bool IsModerator
    {
        get => _isModerator;
        private set
        {
            if (_isModerator != value)
            {
                _isModerator = value;
                OnPropertyChanged(nameof(IsModerator));
            }
        }
    }

    public BottomNavBar()
    {
        InitializeComponent();
        RefreshModeratorStatus();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        RefreshModeratorStatus();
    }

    private void RefreshModeratorStatus()
    {
        try
        {
            var auth = Handler?.MauiContext?.Services.GetService<IAuthService>();
            if (auth != null)
            {
                IsModerator = auth.CurrentUser?.IsModerator == true;
                auth.AuthStateChanged -= OnAuthStateChanged;
                auth.AuthStateChanged += OnAuthStateChanged;
            }
            else
            {
                // Fallback check
                IsModerator = false;
            }
        }
        catch
        {
            IsModerator = false;
        }
    }

    private void OnAuthStateChanged(object? sender, UserProfile? profile)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsModerator = profile?.IsModerator == true;
        });
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BottomNavBar bar)
        {
            bar.OnPropertyChanged(nameof(IsFeedActive));
            bar.OnPropertyChanged(nameof(IsUploadActive));
            bar.OnPropertyChanged(nameof(IsReviewActive));
            bar.OnPropertyChanged(nameof(IsMeActive));
            bar.OnPropertyChanged(nameof(FeedTextColor));
            bar.OnPropertyChanged(nameof(ReviewTextColor));
            bar.OnPropertyChanged(nameof(MeTextColor));
        }
    }

    private async void OnFeedTapped(object? sender, TappedEventArgs e)
    {
        if (!IsFeedActive)
        {
            await Shell.Current.GoToAsync("//feedpage");
        }
    }

    private async void OnUploadTapped(object? sender, TappedEventArgs e)
    {
        var auth = Handler?.MauiContext?.Services.GetService<IAuthService>();
        if (auth?.CurrentUser == null)
        {
            ToastService.Current.Show("Please sign in to upload photos.", ToastType.Info);
            await Shell.Current.GoToAsync("login");
            return;
        }

        await Shell.Current.GoToAsync("//uploadpage");
    }

    private async void OnReviewTapped(object? sender, TappedEventArgs e)
    {
        if (!IsReviewActive)
        {
            await Shell.Current.GoToAsync("//reviewpage");
        }
    }

    private async void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        if (!IsMeActive)
        {
            await Shell.Current.GoToAsync("//profilepage");
        }
    }
}
