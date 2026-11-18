using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace VividImagination.Modules.Media.Services.ImageProcessing;

public class ImageSharpProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageSharpProcessingService> _logger;

    public ImageSharpProcessingService(ILogger<ImageSharpProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessedImageResult> ProcessImageAsync(Stream inputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);

        if (inputStream.CanSeek)
        {
            inputStream.Position = 0;
        }

        using var image = await Image.LoadAsync<Rgb24>(inputStream, cancellationToken);
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        _logger.LogInformation("Processing image of size {Width}x{Height}", originalWidth, originalHeight);

        // 1. Compute BlurHash on a downscaled 64x64 thumbnail for optimal performance and accurate DCT
        using var blurHashSample = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(64, 64),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Bicubic
        }));
        string blurHash = BlurHashGenerator.Encode(blurHashSample, 4, 3);

        // 2. Generate Original (Optimized WebP, Quality = 90)
        var originalVariant = await EncodeVariantAsync(image, originalWidth, 90, "original", cancellationToken);

        // 3. Generate Medium Variant (Max 1080px, Quality = 85)
        var mediumVariant = await EncodeVariantAsync(image, 1080, 85, "medium", cancellationToken);

        // 4. Generate Thumbnail Variant (Max 256px, Quality = 80)
        var thumbnailVariant = await EncodeVariantAsync(image, 256, 80, "thumbnail", cancellationToken);

        return new ProcessedImageResult(
            originalWidth,
            originalHeight,
            blurHash,
            originalVariant,
            mediumVariant,
            thumbnailVariant);
    }

    private static async Task<ImageVariantResult> EncodeVariantAsync(
        Image<Rgb24> sourceImage,
        int maxWidth,
        int quality,
        string variantName,
        CancellationToken cancellationToken)
    {
        using var clone = sourceImage.Clone(ctx =>
        {
            if (sourceImage.Width > maxWidth)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, 0),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Bicubic
                });
            }
        });

        using var memoryStream = new MemoryStream();
        var encoder = new WebpEncoder
        {
            Quality = quality,
            FileFormat = WebpFileFormatType.Lossy
        };

        await clone.SaveAsWebpAsync(memoryStream, encoder, cancellationToken);
        var bytes = memoryStream.ToArray();

        return new ImageVariantResult(
            bytes,
            variantName,
            clone.Width,
            clone.Height,
            bytes.Length,
            "image/webp");
    }
}
