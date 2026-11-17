using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VividImagination.Mobile.ViewModels;

public partial class FeedItemViewModel : ObservableObject
{
    private readonly FeedItem _model;
    private ImageSource? _blurHashImage;

    public FeedItemViewModel(FeedItem model)
    {
        _model = model;
        Upvotes = model.Upvotes;
        Downvotes = model.Downvotes;
        Score = model.Score;
        BlurRequired = model.BlurRequired;
        IsUnsafe = model.IsUnsafe;
        IsRevealed = !model.BlurRequired;
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
    private int _upvotes;

    [ObservableProperty]
    private int _downvotes;

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
    private int _userVote; // 1 = up, -1 = down, 0 = none

    [ObservableProperty]
    private bool _isVoting;

    [ObservableProperty]
    private bool _isReported;

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

    public string FormattedScore => $"{Score:F1} pts";

    [RelayCommand]
    private void ToggleReveal()
    {
        IsRevealed = !IsRevealed;
    }

    public void ApplyVote(int upvotes, int downvotes)
    {
        Upvotes = upvotes;
        Downvotes = downvotes;
        var ageHours = Math.Max(0, (DateTime.UtcNow - UploadedAt).TotalHours);
        Score = (upvotes - downvotes) / Math.Pow(ageHours + 2, 1.5);
        OnPropertyChanged(nameof(FormattedScore));
    }
}
