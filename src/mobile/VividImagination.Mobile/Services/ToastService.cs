namespace VividImagination.Mobile.Services;

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class ToastMessage
{
    public required string Text { get; init; }
    public ToastType Type { get; init; } = ToastType.Info;
}

public interface IToastService
{
    event EventHandler<ToastMessage>? ToastRequested;
    void Show(string message, ToastType type = ToastType.Info);
}

public sealed class ToastService : IToastService
{
    public static ToastService Current { get; } = new();

    public event EventHandler<ToastMessage>? ToastRequested;

    public void Show(string message, ToastType type = ToastType.Info)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ToastRequested?.Invoke(this, new ToastMessage { Text = message, Type = type });
        });
    }
}
