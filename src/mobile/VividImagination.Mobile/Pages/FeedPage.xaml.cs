using VividImagination.Mobile.ViewModels;

namespace VividImagination.Mobile.Pages;

public partial class FeedPage : ContentPage
{
    private readonly FeedViewModel _viewModel;
    private bool _isPulsing;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartSkeletonPulse();

        if (_viewModel.Items.Count == 0)
        {
            await _viewModel.LoadFeedCommand.ExecuteAsync(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPulsing = false;
    }

    private async void StartSkeletonPulse()
    {
        if (_isPulsing) return;
        _isPulsing = true;

        while (_isPulsing && _viewModel.IsBusy)
        {
            await SkeletonView.FadeToAsync(0.4, 600, Easing.SinInOut);
            await SkeletonView.FadeToAsync(1.0, 600, Easing.SinInOut);
        }
    }
}
