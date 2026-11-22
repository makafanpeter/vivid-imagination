using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.Pages.Controls;

public partial class TopToastView : ContentView
{
    private CancellationTokenSource? _dismissCts;

    public TopToastView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        ToastService.Current.ToastRequested -= OnToastRequested;
        ToastService.Current.ToastRequested += OnToastRequested;
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        ToastService.Current.ToastRequested -= OnToastRequested;
        _dismissCts?.Cancel();
    }

    private async void OnToastRequested(object? sender, ToastMessage msg)
    {
        _dismissCts?.Cancel();
        _dismissCts = new CancellationTokenSource();
        var ct = _dismissCts.Token;

        ToastText.Text = msg.Text;
        ToastIcon.Text = msg.Type switch
        {
            ToastType.Success => "✓",
            ToastType.Warning => "⚠️",
            ToastType.Error => "✕",
            _ => "✨"
        };

        ToastContainer.IsVisible = true;
        await ToastContainer.FadeToAsync(1, 200, Easing.CubicOut);

        try
        {
            await Task.Delay(2500, ct);
            if (!ct.IsCancellationRequested)
            {
                await ToastContainer.FadeToAsync(0, 250, Easing.CubicIn);
                ToastContainer.IsVisible = false;
            }
        }
        catch (TaskCanceledException)
        {
            // Another toast arrived
        }
    }
}
