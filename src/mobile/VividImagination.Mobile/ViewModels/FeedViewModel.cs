using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.OpenApi;
using VividImagination.Mobile.Services;

namespace VividImagination.Mobile.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly IVividImaginationClient _apiClient;
    private readonly IAuthService _authService;
    private readonly IPhotoLoaderService _photoLoader;
    private readonly IFeedTelemetryService _telemetryService;
    private int _currentPage = 1;
    private const int PageSize = 10;
    private bool _hasMoreItems = true;

    public FeedViewModel(
        IVividImaginationClient apiClient,
        IAuthService authService,
        IPhotoLoaderService photoLoader,
        IFeedTelemetryService telemetryService)
    {
        _apiClient = apiClient;
        _authService = authService;
        _photoLoader = photoLoader;
        _telemetryService = telemetryService;
        Items = [];

        _authService.AuthStateChanged += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _ = LoadFeedAsync();
            });
        };
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
    public async Task NavigateToUploadAsync()
    {
        await Shell.Current.GoToAsync("//uploadpage");
    }

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

            var safeMode = _authService.CurrentUser?.SafeBrowsingMode ?? SafeBrowsingMode.AlwaysShowSensitive;
            var isAuthenticated = _authService.CurrentUser != null || await _authService.IsAuthenticatedAsync();

            ICollection<FeedItemDto> rawItems;
            if (isAuthenticated)
            {
                try
                {
                    rawItems = await _apiClient.GetPersonalizedFeedAsync(_currentPage, PageSize, safeMode.ToString());
                }
                catch (ApiException ex) when (ex.StatusCode == 401)
                {
                    rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize, safeMode.ToString());
                }
            }
            else
            {
                rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize, safeMode.ToString());
            }

            Items.Clear();

            if (rawItems.Count > 0)
            {
                var newItems = new List<FeedItemViewModel>();
                foreach (var item in rawItems)
                {
                    if (safeMode == SafeBrowsingMode.HideSensitive && item.IsUnsafe)
                    {
                        continue;
                    }

                    var vm = CreateFeedItemViewModel(item, safeMode);
                    newItems.Add(vm);
                    Items.Add(vm);
                }

                RecordImpressions(newItems, 0);
            }

            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load feed: {ex.Message}";
            IsEmpty = Items.Count == 0;
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

            var safeMode = _authService.CurrentUser?.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive;
            var isAuthenticated = _authService.CurrentUser != null || await _authService.IsAuthenticatedAsync();

            ICollection<FeedItemDto> rawItems;
            if (isAuthenticated)
            {
                try
                {
                    rawItems = await _apiClient.GetPersonalizedFeedAsync(_currentPage, PageSize, safeMode.ToString());
                }
                catch (ApiException ex) when (ex.StatusCode == 401)
                {
                    rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize, safeMode.ToString());
                }
            }
            else
            {
                rawItems = await _apiClient.GetRankedFeedAsync(_currentPage, PageSize, safeMode.ToString());
            }

            if (rawItems.Count == 0)
            {
                _hasMoreItems = false;
                return;
            }

            var startPosition = Items.Count;
            var newItems = new List<FeedItemViewModel>();
            foreach (var item in rawItems)
            {
                if (safeMode == SafeBrowsingMode.HideSensitive && item.IsUnsafe)
                {
                    continue;
                }

                var vm = CreateFeedItemViewModel(item, safeMode);
                newItems.Add(vm);
                Items.Add(vm);
            }

            RecordImpressions(newItems, startPosition);
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

    private void RecordImpressions(IEnumerable<FeedItemViewModel> newItems, int startingPosition)
    {
        var position = startingPosition;
        foreach (var item in newItems)
        {
            _ = _telemetryService.RecordInteractionAsync(
                item.PhotoId,
                item.UserId,
                "impression",
                feedPosition: position++,
                tags: item.TagsList);
        }
    }

    [RelayCommand]
    public async Task UpvoteAsync(FeedItemViewModel? item)
    {
        if (item == null) return;

        var user = _authService.CurrentUser ?? await _authService.GetCurrentUserProfileAsync();
        if (user == null)
        {
            ToastService.Current.Show("Sign in to vote or report", ToastType.Info);
            return;
        }

        _ = _telemetryService.RecordInteractionAsync(
            item.PhotoId,
            item.UserId,
            "upvote",
            feedPosition: Items.IndexOf(item),
            tags: item.TagsList);

        await VoteInternalAsync(item, 1);
    }

    [RelayCommand]
    public async Task DownvoteAsync(FeedItemViewModel? item)
    {
        if (item == null) return;

        var user = _authService.CurrentUser ?? await _authService.GetCurrentUserProfileAsync();
        if (user == null)
        {
            ToastService.Current.Show("Sign in to vote or report", ToastType.Info);
            return;
        }

        _ = _telemetryService.RecordInteractionAsync(
            item.PhotoId,
            item.UserId,
            "downvote",
            feedPosition: Items.IndexOf(item),
            tags: item.TagsList);

        await VoteInternalAsync(item, -1);

        // Tap down turns red and shows "Hidden from your feed" note, removing from feed
        ToastService.Current.Show("Hidden from your feed", ToastType.Info);
        Items.Remove(item);
        IsEmpty = Items.Count == 0;
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

            VoteResult? result;
            if (newVote == 0)
            {
                result = await _apiClient.CancelVoteOnPhotoAsync(item.PhotoId);
            }
            else
            {
                result = await _apiClient.VoteOnPhotoAsync(item.PhotoId, new VoteRequest { Value = newVote });
            }

            if (result != null)
            {
                item.ApplyVote(result.Upvotes, result.Downvotes);
            }
        }
        catch
        {
            // Keep local state responsive in offline/demo mode
            if (value == 1)
            {
                item.ApplyVote(item.Upvotes + (item.UserVote == 1 ? 1 : -1), item.Downvotes);
            }
            else if (value == -1)
            {
                item.ApplyVote(item.Upvotes, item.Downvotes + (item.UserVote == -1 ? 1 : -1));
            }
        }
        finally
        {
            item.IsVoting = false;
        }
    }

    [RelayCommand]
    public async Task ReportAsync(FeedItemViewModel? item)
    {
        if (item == null) return;

        var user = _authService.CurrentUser ?? await _authService.GetCurrentUserProfileAsync();
        if (user == null)
        {
            ToastService.Current.Show("Sign in to vote or report", ToastType.Info);
            return;
        }

        if (item.IsReported)
        {
            ToastService.Current.Show("You already reported this", ToastType.Warning);
            return;
        }

        item.IsReported = true;
        ToastService.Current.Show("Thanks — our moderators will take a look.", ToastType.Success);

        try
        {
            await _apiClient.ReportPhotoAsync(item.PhotoId, new ReportRequest { Reason = "Community Flag" });
        }
        catch
        {
            // Local report handled
        }
    }

    private FeedItemViewModel CreateFeedItemViewModel(FeedItemDto item, SafeBrowsingMode safeMode)
    {
        var vm = new FeedItemViewModel(item, _photoLoader)
        {
            UpvoteHandler = UpvoteAsync,
            DownvoteHandler = DownvoteAsync,
            ReportHandler = ReportAsync
        };

        if (safeMode == SafeBrowsingMode.AlwaysShowSensitive)
        {
            vm.IsRevealed = true;
        }

        return vm;
    }
}
