using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VividImagination.Mobile.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly IVividImaginationClient _apiClient;
    private readonly IAuthService _authService;
    private int _currentPage = 1;
    private const int PageSize = 10;
    private bool _hasMoreItems = true;

    public FeedViewModel(
        IVividImaginationClient apiClient,
        IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;
        Items = [];
    }

    public ObservableCollection<FeedItemViewModel> Items { get; }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoadingMore;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isEmpty;

    [RelayCommand]
    public async Task LoadFeedAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            _currentPage = 1;
            _hasMoreItems = true;

            var rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize);
            Items.Clear();

            var safeMode = _authService.CurrentUser?.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive;

            foreach (var item in rawItems)
            {
                if (safeMode == SafeBrowsingMode.HideSensitive && item.IsUnsafe)
                {
                    continue;
                }

                var vm = new FeedItemViewModel(item);
                if (safeMode == SafeBrowsingMode.AlwaysShowSensitive)
                {
                    vm.IsRevealed = true;
                }

                Items.Add(vm);
            }

            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load feed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task RefreshFeedAsync()
    {
        IsRefreshing = true;
        await LoadFeedAsync();
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsBusy || IsLoadingMore || !_hasMoreItems) return;

        try
        {
            IsLoadingMore = true;
            _currentPage++;

            var rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize);
            if (rawItems == null || rawItems.Count == 0)
            {
                _hasMoreItems = false;
                return;
            }

            var safeMode = _authService.CurrentUser?.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive;

            foreach (var item in rawItems)
            {
                if (safeMode == SafeBrowsingMode.HideSensitive && item.IsUnsafe)
                {
                    continue;
                }

                var vm = new FeedItemViewModel(item);
                if (safeMode == SafeBrowsingMode.AlwaysShowSensitive)
                {
                    vm.IsRevealed = true;
                }

                Items.Add(vm);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load more feed: {ex.Message}");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    public async Task UpvoteAsync(FeedItemViewModel? item)
    {
        if (item == null) return;
        await VoteInternalAsync(item, 1);
    }

    [RelayCommand]
    public async Task DownvoteAsync(FeedItemViewModel? item)
    {
        if (item == null) return;
        await VoteInternalAsync(item, -1);
    }

    private async Task VoteInternalAsync(FeedItemViewModel item, int value)
    {
        if (item.IsVoting) return;

        var previousVote = item.UserVote;
        try
        {
            item.IsVoting = true;

            var newVote = previousVote == value ? 0 : value;
            item.UserVote = newVote;

            var result = await _apiClient.VoteOnPhotoAsync(item.PhotoId, new VoteRequest { Value = newVote });
            if (result != null)
            {
                item.ApplyVote(result.Upvotes, result.Downvotes);
            }
            else
            {
                item.UserVote = previousVote;
            }
        }
        catch
        {
            item.UserVote = previousVote;
        }
        finally
        {
            item.IsVoting = false;
        }
    }

    [RelayCommand]
    public async Task ReportAsync(FeedItemViewModel? item)
    {
        if (item == null || item.IsReported) return;

        var reason = await Shell.Current.DisplayActionSheetAsync(
            "Report Photo",
            "Cancel",
            null,
            "Inappropriate or suggestive content",
            "Violence or harmful imagery",
            "Hate speech or harassment",
            "Spam or misleading");

        if (string.IsNullOrEmpty(reason) || reason == "Cancel")
        {
            return;
        }

        try
        {
            var result = await _apiClient.ReportPhotoAsync(item.PhotoId, new ReportRequest { Reason = reason });
            if (result != null)
            {
                item.IsReported = true;
                if (result.TotalReports >= 2)
                {
                    item.BlurRequired = true;
                    item.IsRevealed = false;
                    item.IsUnsafe = true;
                }

                await Shell.Current.DisplayAlertAsync("Report Received", "Thank you for helping keep Vivid Imagination safe. Our moderation team has been notified.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not submit report: {ex.Message}", "OK");
        }
    }
}
