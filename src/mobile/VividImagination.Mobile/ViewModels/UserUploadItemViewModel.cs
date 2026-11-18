using CommunityToolkit.Mvvm.ComponentModel;

namespace VividImagination.Mobile.ViewModels;

public partial class UserUploadItemViewModel : ObservableObject
{
    public Guid PhotoId { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _caption = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private string _status = "Live"; // Checking, Live, Sensitive, Removed

    public Color StatusColor => Status switch
    {
        "Sensitive" => Color.FromArgb("#F59E0B"), // Amber
        "Removed" => Color.FromArgb("#EF4444"),   // Red
        "Checking" => Color.FromArgb("#60A5FA"),  // Soft Blue
        _ => Color.FromArgb("#22C55E")            // Live Green
    };
}
