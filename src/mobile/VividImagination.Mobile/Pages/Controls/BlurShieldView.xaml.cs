using VividImagination.Mobile.ViewModels;

namespace VividImagination.Mobile.Pages.Controls;

public partial class BlurShieldView : ContentView
{
    public BlurShieldView()
    {
        InitializeComponent();
    }

    private async void OnShowContentClicked(object sender, EventArgs e)
    {
        // 500ms fade un-blurs the photo in place
        await ShieldOverlay.FadeToAsync(0, 500, Easing.CubicOut);
        ShieldOverlay.IsVisible = false;

        if (BindingContext is FeedItemViewModel vm)
        {
            vm.RevealContent();
        }
    }
}
