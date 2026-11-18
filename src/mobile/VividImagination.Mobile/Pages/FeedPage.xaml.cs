namespace VividImagination.Mobile.Pages;

public partial class FeedPage : ContentPage
{
    private readonly FeedViewModel _viewModel;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Items.Count == 0)
        {
            await _viewModel.LoadFeedCommand.ExecuteAsync(null);
        }
    }
}
