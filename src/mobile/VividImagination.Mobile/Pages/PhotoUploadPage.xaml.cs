namespace VividImagination.Mobile.Pages;

public partial class PhotoUploadPage : ContentPage
{
    public PhotoUploadPage(PhotoUploadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
