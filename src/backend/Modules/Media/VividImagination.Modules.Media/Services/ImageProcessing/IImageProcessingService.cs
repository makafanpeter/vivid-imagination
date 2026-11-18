namespace VividImagination.Modules.Media.Services.ImageProcessing;

public record ImageVariantResult(
    byte[] Bytes,
    string VariantName,
    int Width,
    int Height,
    long ContentLength,
    string ContentType = "image/webp");

public record ProcessedImageResult(
    int OriginalWidth,
    int OriginalHeight,
    string BlurHash,
    ImageVariantResult Original,
    ImageVariantResult Medium,
    ImageVariantResult Thumbnail);

public interface IImageProcessingService
{
    Task<ProcessedImageResult> ProcessImageAsync(Stream inputStream, CancellationToken cancellationToken = default);
}
