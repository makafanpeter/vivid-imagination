using VividImagination.Mobile.Models;
using VividImagination.Mobile.OpenApi;

namespace VividImagination.Mobile.Services;

public interface IPhotoLoaderService
{
    Task<ImageSource?> LoadPhotoAsync(Guid photoId, PhotoVariant variant = PhotoVariant.Medium, string? blurHash = null, CancellationToken ct = default);
    Task<ImageSource?> LoadPhotoAsync(Guid photoId, string? blurHash, CancellationToken ct = default);
    string GetCachedFilePath(Guid photoId, PhotoVariant variant = PhotoVariant.Medium);
}

public sealed class PhotoLoaderService(IVividImaginationClient apiClient) : IPhotoLoaderService
{
    private readonly string _cacheDir = Path.Combine(FileSystem.CacheDirectory, "photos");

    public string GetCachedFilePath(Guid photoId, PhotoVariant variant = PhotoVariant.Medium) =>
        Path.Combine(_cacheDir, $"{photoId}_{variant.ToString().ToLowerInvariant()}.bin");

    public Task<ImageSource?> LoadPhotoAsync(Guid photoId, string? blurHash, CancellationToken ct = default) =>
        LoadPhotoAsync(photoId, PhotoVariant.Medium, blurHash, ct);

    public async Task<ImageSource?> LoadPhotoAsync(Guid photoId, PhotoVariant variant = PhotoVariant.Medium, string? blurHash = null, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }

            var filePath = GetCachedFilePath(photoId, variant);

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                var cachedBytes = await File.ReadAllBytesAsync(filePath, ct);
                return ImageSource.FromStream(() => new MemoryStream(cachedBytes));
            }

            var variantName = variant.ToString().ToLowerInvariant();

            // Download using authenticated OpenAPI client with variant query parameter
            using var response = await apiClient.DownloadPhotoAsync(photoId, variantName, ct);
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
            System.Diagnostics.Debug.WriteLine($"Failed to download photo {photoId} (variant {variant}): {ex}");
        }

        // Graceful fallback to BlurHash placeholder
        if (!string.IsNullOrWhiteSpace(blurHash))
        {
            return BlurHashDecoder.DecodeToImageSource(blurHash, 32, 32);
        }

        return null;
    }
}
