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
    private int _reportCount = 1;

    [ObservableProperty]
    private double _adultScore = 0.0;

    [ObservableProperty]
    private double _violenceScore = 0.0;

    [ObservableProperty]
    private double _hateScore = 0.0;

    public string ReportText => $"🚩 {ReportCount} reports";
    public string AdultPercentText => $"Adult {(int)(AdultScore * 100)}%";
    public string ViolencePercentText => $"Violence {(int)(ViolenceScore * 100)}%";
    public string HatePercentText => $"Hate {(int)(HateScore * 100)}%";
}
