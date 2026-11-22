using VividImagination.Mobile.OpenApi;

namespace VividImagination.Mobile.Services;

public interface IPhotoLoaderService
{
    Task<ImageSource?> LoadPhotoAsync(Guid photoId, string? blurHash = null, CancellationToken ct = default);
    string GetCachedFilePath(Guid photoId);
}

public sealed class PhotoLoaderService(IVividImaginationClient apiClient) : IPhotoLoaderService
{
    private readonly string _cacheDir = Path.Combine(FileSystem.CacheDirectory, "photos");

    public string GetCachedFilePath(Guid photoId) => Path.Combine(_cacheDir, $"{photoId}.bin");

    public async Task<ImageSource?> LoadPhotoAsync(Guid photoId, string? blurHash = null, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }

            var filePath = GetCachedFilePath(photoId);

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                var cachedBytes = await File.ReadAllBytesAsync(filePath, ct);
                return ImageSource.FromStream(() => new MemoryStream(cachedBytes));
            }

            // Download using authenticated OpenAPI client
            using var response = await apiClient.DownloadPhotoAsync(photoId, ct);
            if (response?.Stream != null)
            {
                using var ms = new MemoryStream();
                await response.Stream.CopyToAsync(ms, ct);
                var bytes = ms.ToArray();
                if (bytes.Length > 0)
                {
                    try
                    {
                        await File.WriteAllBytesAsync(filePath, bytes, ct);
                    }
                    catch
                    {
                        // Ignore cache write concurrency errors
                    }

                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to download photo {photoId}: {ex.Message}");
        }

        // Graceful fallback to BlurHash placeholder
        if (!string.IsNullOrWhiteSpace(blurHash))
        {
            return BlurHashDecoder.DecodeToImageSource(blurHash, 32, 32);
        }

        return null;
    }
}
