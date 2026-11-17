using System.Net.Http.Headers;
using System.Text.Json;

namespace VividImagination.Mobile.Services;

public interface IMediaService
{
    Task<bool> UploadPhotoAsync(Stream fileStream, string fileName, string contentType, string? caption, string? name, CancellationToken ct = default);
    Task<bool> DeletePhotoAsync(Guid photoId, CancellationToken ct = default);
}

public sealed class MediaService(HttpClient httpClient) : IMediaService
{
    public async Task<bool> UploadPhotoAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? caption,
        string? name,
        CancellationToken ct = default)
    {
        try
        {
            using var formData = new MultipartFormDataContent();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formData.Add(fileContent, "FormFile", fileName);

            if (!string.IsNullOrEmpty(name))
            {
                formData.Add(new StringContent(name), "Name");
            }

            if (!string.IsNullOrEmpty(caption))
            {
                formData.Add(new StringContent(caption), "Description");
            }

            formData.Add(new StringContent("photos"), "Container");

            var response = await httpClient.PostAsync(AppConfig.MediaApiUrl, formData, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error uploading photo: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        try
        {
            var url = $"{AppConfig.MediaApiUrl}/{photoId}";
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting photo: {ex.Message}");
            return false;
        }
    }
}
