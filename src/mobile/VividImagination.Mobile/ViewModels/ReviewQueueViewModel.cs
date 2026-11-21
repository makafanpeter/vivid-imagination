using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.OpenApi;
using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.ViewModels;

public partial class ReviewQueueViewModel : ObservableObject
{
    private readonly IVividImaginationClient _apiClient;
    private readonly IAuthService _authService;

    public ReviewQueueViewModel(
        IVividImaginationClient apiClient,
        IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;
        QueueItems = [];
        LoadQueue();
    }

    public ObservableCollection<ReviewItemViewModel> QueueItems { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaitingCountText))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private int _waitingCount;

    public string WaitingCountText => $"Review queue ({QueueItems.Count})";

    public bool IsEmpty => QueueItems.Count == 0;

    public bool IsModerator => _authService.CurrentUser?.IsModerator == true;

    private void LoadQueue()
    {
        // No sample data - queue starts empty until populated from API
        WaitingCount = QueueItems.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task ApproveAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            await _apiClient.UpdateModerationVerdictAsync(item.PhotoId, new UpdateModerationVerdictRequest
            {
                Verdict = "Approved",
                BlurRequired = false,
                Reason = "Approved by moderator"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to record approve verdict: {ex.Message}");
        }

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        OnPropertyChanged(nameof(IsEmpty));
        ToastService.Current.Show("Photo approved for feed", ToastType.Success);
    }

    [RelayCommand]
    public async Task MarkSensitiveAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            await _apiClient.UpdateModerationVerdictAsync(item.PhotoId, new UpdateModerationVerdictRequest
            {
                Verdict = "TaggedUnsafe",
                BlurRequired = true,
                Reason = "Marked as sensitive by moderator"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to record sensitive verdict: {ex.Message}");
        }

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        OnPropertyChanged(nameof(IsEmpty));
        ToastService.Current.Show("Marked as Sensitive with blur cover", ToastType.Warning);
    }

    [RelayCommand]
    public async Task RejectAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            await _apiClient.UpdateModerationVerdictAsync(item.PhotoId, new UpdateModerationVerdictRequest
            {
                Verdict = "Rejected",
                BlurRequired = true,
                Reason = "Rejected by moderator"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to record reject verdict: {ex.Message}");
        }

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        OnPropertyChanged(nameof(IsEmpty));
        ToastService.Current.Show("Photo rejected and removed from community", ToastType.Error);
    }
}
