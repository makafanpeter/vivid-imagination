namespace VividImagination.Mobile.Pages.Controls;

public partial class OfflineFallbackView : ContentView
{
    public event EventHandler? RetryRequested;

    public OfflineFallbackView()
    {
        InitializeComponent();
    }

    private void OnRetryClicked(object sender, EventArgs e)
    {
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }
}
