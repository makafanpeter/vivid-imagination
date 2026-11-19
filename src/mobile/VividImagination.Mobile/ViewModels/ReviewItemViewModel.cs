using CommunityToolkit.Mvvm.ComponentModel;

namespace VividImagination.Mobile.ViewModels;

public partial class ReviewItemViewModel : ObservableObject
{
    public Guid PhotoId { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _caption = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReportText))]
    [NotifyPropertyChangedFor(nameof(ReportCountText))]
    private int _reportCount = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdultPercentText))]
    [NotifyPropertyChangedFor(nameof(AdultScoreText))]
    private double _adultScore = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ViolencePercentText))]
    [NotifyPropertyChangedFor(nameof(ViolenceScoreText))]
    private double _violenceScore = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HatePercentText))]
    [NotifyPropertyChangedFor(nameof(HateScoreText))]
    private double _hateScore = 0.0;

    public string ReportText => $"🚩 {ReportCount} reports";
    public string ReportCountText => ReportText;

    public string AdultPercentText => $"Adult {(int)(AdultScore * 100)}%";
    public string AdultScoreText => AdultPercentText;

    public string ViolencePercentText => $"Violence {(int)(ViolenceScore * 100)}%";
    public string ViolenceScoreText => ViolencePercentText;

    public string HatePercentText => $"Hate {(int)(HateScore * 100)}%";
    public string HateScoreText => HatePercentText;
}
