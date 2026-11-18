using VividImagination.Mobile.ViewModels;

namespace VividImagination.Mobile.Pages;

public partial class ReviewQueuePage : ContentPage
{
    public ReviewQueuePage(ReviewQueueViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
