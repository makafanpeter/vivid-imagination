using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VividImagination.Mobile.ViewModels;

public partial class PhotoUploadViewModel(IMediaService mediaService) : ObservableObject
{
    private Stream? _selectedStream;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _caption = string.Empty;

    [ObservableProperty]
    private ImageSource? _previewImage;

    [ObservableProperty]
    private string? _selectedFileName;

    [ObservableProperty]
    private string? _contentType;

    [ObservableProperty]
    private bool _hasSelectedPhoto;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private string? _statusMessage;

    [RelayCommand]
    public async Task PickPhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Select a Photo to Share"
                });

                var photo = photos?.FirstOrDefault();
                await LoadSelectedPhotoAsync(photo);
            }
            else
            {
                StatusMessage = "Photo picker is not supported on this device.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to pick photo: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task TakePhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                await LoadSelectedPhotoAsync(photo);
            }
            else
            {
                StatusMessage = "Camera is not supported on this device.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to capture photo: {ex.Message}";
        }
    }

    private async Task LoadSelectedPhotoAsync(FileResult? photo)
    {
        if (photo == null) return;

        SelectedFileName = photo.FileName;
        ContentType = photo.ContentType ?? "image/jpeg";

        var memoryStream = new MemoryStream();
        using (var stream = await photo.OpenReadAsync())
        {
            await stream.CopyToAsync(memoryStream);
        }

        memoryStream.Position = 0;
        _selectedStream?.Dispose();
        _selectedStream = memoryStream;

        PreviewImage = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
        HasSelectedPhoto = true;
        StatusMessage = null;

        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = Path.GetFileNameWithoutExtension(photo.FileName);
        }
    }

    [RelayCommand]
    public async Task UploadAsync()
    {
        if (_selectedStream == null || !HasSelectedPhoto)
        {
            StatusMessage = "Please select or take a photo first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Caption))
        {
            StatusMessage = "Please enter a caption.";
            return;
        }

        try
        {
            IsUploading = true;
            StatusMessage = "Uploading & running Amazon Rekognition moderation...";

            _selectedStream.Position = 0;
            var success = await mediaService.UploadPhotoAsync(
                _selectedStream,
                SelectedFileName ?? "photo.jpg",
                ContentType ?? "image/jpeg",
                Caption,
                Title);

            if (success)
            {
                StatusMessage = "Photo published successfully!";
                await Task.Delay(800);
                Clear();
                // Navigate to feed
                await Shell.Current.GoToAsync("//feed");
            }
            else
            {
                StatusMessage = "Failed to upload photo. Please check your connection and login status.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload error: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    public void Clear()
    {
        _selectedStream?.Dispose();
        _selectedStream = null;
        PreviewImage = null;
        HasSelectedPhoto = false;
        Title = string.Empty;
        Caption = string.Empty;
        SelectedFileName = null;
        StatusMessage = null;
    }
}
