using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace VividImagination.Mobile.ViewModels;

public partial class FeedItemViewModel : ObservableObject
{
    private readonly FeedItemDto _model;
    private ImageSource? _blurHashImage;

    public FeedItemViewModel(FeedItemDto model)
    {
        _model = model;
        Upvotes = model.Upvotes;
        Downvotes = model.Downvotes;
        Score = model.Score;
        BlurRequired = model.BlurRequired;
        IsUnsafe = model.IsUnsafe;
        IsRevealed = !model.BlurRequired;
        ExtractTags();
    }

    public Guid Id => _model.Id;
    public Guid PhotoId => _model.PhotoId;
    public Guid UserId => _model.UserId;
    public string Caption => _model.Caption;
    public string BlurHash => _model.BlurHash;
    public string? SafetyCategory => _model.SafetyCategory;
    public string? SensitiveContentWarning => _model.SensitiveContentWarning ?? (_model.IsUnsafe ? "Sensitive Content" : null);
    public DateTime UploadedAt => _model.UploadedAt;

    public string ImageUrl => AppConfig.GetDownloadUrl(_model.PhotoId);

    public ImageSource? BlurHashImageSource
    {
        get
        {
            if (_blurHashImage == null && !string.IsNullOrWhiteSpace(_model.BlurHash))
            {
                _blurHashImage = BlurHashDecoder.DecodeToImageSource(_model.BlurHash, 32, 32);
            }
            return _blurHashImage;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetScore))]
    private int _upvotes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetScore))]
    private int _downvotes;

    public int NetScore => Upvotes - Downvotes;

    [ObservableProperty]
    private double _score;

    [ObservableProperty]
    private bool _blurRequired;

    [ObservableProperty]
    private bool _isUnsafe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShieldActive))]
    private bool _isRevealed;

    public bool IsShieldActive => BlurRequired && !IsRevealed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpvoted))]
    [NotifyPropertyChangedFor(nameof(IsDownvoted))]
    [NotifyPropertyChangedFor(nameof(UpvoteGlyph))]
    [NotifyPropertyChangedFor(nameof(DownvoteGlyph))]
    private int _userVote; // 1 = up, -1 = down, 0 = none

    public bool IsUpvoted => UserVote == 1;
    public bool IsDownvoted => UserVote == -1;
    public string UpvoteGlyph => IsUpvoted ? "▲" : "△";
    public string DownvoteGlyph => IsDownvoted ? "▼" : "▽";

    [ObservableProperty]
    private bool _isVoting;

    [ObservableProperty]
    private bool _isReported;

    [ObservableProperty]
    private bool _isHidden;

    [ObservableProperty]
    private string _tagsLine = string.Empty;

    public string FormattedDate
    {
        get
        {
            var span = DateTime.UtcNow - UploadedAt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return UploadedAt.ToString("MMM d, yyyy");
        }
    }

    private void ExtractTags()
    {
        if (string.IsNullOrWhiteSpace(Caption))
        {
            TagsLine = "#vivid #community";
            return;
        }

        var matches = Regex.Matches(Caption, @"#\w+");
        if (matches.Count > 0)
        {
            TagsLine = string.Join(" ", matches.Select(m => m.Value));
        }
        else
        {
            TagsLine = "#vivid #moments";
        }
    }

    [RelayCommand]
    public void RevealContent()
    {
        IsRevealed = true;
    }

    public Func<FeedItemViewModel, Task>? UpvoteHandler { get; set; }
    public Func<FeedItemViewModel, Task>? DownvoteHandler { get; set; }
    public Func<FeedItemViewModel, Task>? ReportHandler { get; set; }

    [RelayCommand]
    public async Task UpvoteAsync()
    {
        if (UpvoteHandler != null)
        {
            await UpvoteHandler(this);
        }
    }

    [RelayCommand]
    public async Task DownvoteAsync()
    {
        if (DownvoteHandler != null)
        {
            await DownvoteHandler(this);
        }
    }

    [RelayCommand]
    public async Task ReportAsync()
    {
        if (ReportHandler != null)
        {
            await ReportHandler(this);
        }
    }

    public void ApplyVote(int upvotes, int downvotes)
    {
        Upvotes = upvotes;
        Downvotes = downvotes;
        var ageHours = Math.Max(0, (DateTime.UtcNow - UploadedAt).TotalHours);
        Score = (upvotes - downvotes) / Math.Pow(ageHours + 2, 1.5);
        OnPropertyChanged(nameof(NetScore));
    }
}
