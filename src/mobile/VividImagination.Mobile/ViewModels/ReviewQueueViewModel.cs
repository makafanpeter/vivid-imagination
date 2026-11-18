using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private int _waitingCount;

    public string WaitingCountText => $"Review queue ({QueueItems.Count})";

    public bool IsModerator => _authService.CurrentUser?.IsModerator == true;

    private void LoadQueue()
    {
        if (QueueItems.Count > 0) return;

        QueueItems.Add(new ReviewItemViewModel
        {
            PhotoId = Guid.NewGuid(),
            Caption = "Flagged nightlife snapshot with intense crowd motion #club #night",
            ImageUrl = "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=600",
            ReportCount = 3,
            AdultScore = 0.88,
            ViolenceScore = 0.12,
            HateScore = 0.04
        });

        QueueItems.Add(new ReviewItemViewModel
        {
            PhotoId = Guid.NewGuid(),
            Caption = "Graffiti wall with controversial symbols #streetart #urban",
            ImageUrl = "https://images.unsplash.com/photo-1517524008697-84bbe3c3fd98?w=600",
            ReportCount = 2,
            AdultScore = 0.05,
            ViolenceScore = 0.22,
            HateScore = 0.74
        });

        QueueItems.Add(new ReviewItemViewModel
        {
            PhotoId = Guid.NewGuid(),
            Caption = "High-speed rally collision impact test #motorsport #crash",
            ImageUrl = "https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?w=600",
            ReportCount = 4,
            AdultScore = 0.02,
            ViolenceScore = 0.91,
            HateScore = 0.01
        });

        WaitingCount = QueueItems.Count;
    }

    [RelayCommand]
    public async Task ApproveAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        ToastService.Current.Show("Photo approved for feed", ToastType.Success);
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task MarkSensitiveAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        ToastService.Current.Show("Marked as Sensitive with blur cover", ToastType.Warning);
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task RejectAsync(ReviewItemViewModel? item)
    {
        if (item == null) return;

        QueueItems.Remove(item);
        WaitingCount = QueueItems.Count;
        ToastService.Current.Show("Photo rejected and removed from community", ToastType.Error);
        await Task.CompletedTask;
    }
}
