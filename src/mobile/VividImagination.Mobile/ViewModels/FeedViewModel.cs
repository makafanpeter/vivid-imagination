using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VividImagination.Mobile.Services;

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

            if (rawItems != null && rawItems.Count > 0)
            {
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
            else
            {
                // Fallback demo items for visual preview if database is fresh
                PopulateDemoFeed(safeMode);
            }

            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load feed: {ex.Message}";
            if (Items.Count == 0)
            {
                PopulateDemoFeed(_authService.CurrentUser?.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive);
            }
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private void PopulateDemoFeed(SafeBrowsingMode safeMode)
    {
        var sample1 = new FeedItemDto
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Caption = "Golden hour glowing over the mountain ridges #nature #sunset #serenity",
            BlurHash = "LEHV6nWB2yk8pyo0adR*.7kCMdnj",
            Upvotes = 42,
            Downvotes = 3,
            Score = 39.0,
            IsUnsafe = false,
            BlurRequired = false,
            UploadedAt = DateTime.UtcNow.AddHours(-1.5)
        };

        var sample2 = new FeedItemDto
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Caption = "Intense combat sports arena action #boxing #energy #sports",
            BlurHash = "L6PZfSi_.AyE_3t7t7R**0o#DgR4",
            Upvotes = 88,
            Downvotes = 12,
            Score = 76.0,
            IsUnsafe = true,
            BlurRequired = true,
            SafetyCategory = "Suggestive & Graphic Imagery",
            SensitiveContentWarning = "Sensitive Content",
            UploadedAt = DateTime.UtcNow.AddHours(-3)
        };

        var sample3 = new FeedItemDto
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Caption = "Neon night reflections across the Tokyo crossing #city #cyberpunk #tokyo",
            BlurHash = "LBC~8q9G00%M~qofbHx]00?b?bM{",
            Upvotes = 115,
            Downvotes = 5,
            Score = 110.0,
            IsUnsafe = false,
            BlurRequired = false,
            UploadedAt = DateTime.UtcNow.AddHours(-5)
        };

        var samples = new[] { sample1, sample2, sample3 };
        foreach (var item in samples)
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

    [RelayCommand]
    public async Task RefreshFeedAsync()
    {
        IsRefreshing = true;
        await LoadFeedAsync();
    }

    [RelayCommand]
    public async Task NavigateToUploadAsync()
    {
        await Shell.Current.GoToAsync("//upload");
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

        if (_authService.CurrentUser == null)
        {
            ToastService.Current.Show("Sign in to vote or report", ToastType.Info);
            return;
        }

        await VoteInternalAsync(item, 1);
    }

    [RelayCommand]
    public async Task DownvoteAsync(FeedItemViewModel? item)
    {
        if (item == null) return;

        if (_authService.CurrentUser == null)
        {
            ToastService.Current.Show("Sign in to vote or report", ToastType.Info);
            return;
        }

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

            var result = await _apiClient.VoteOnPhotoAsync(item.PhotoId, new VoteRequest { Value = newVote });
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

        if (_authService.CurrentUser == null)
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
}
