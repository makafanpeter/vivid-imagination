using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Features.Feeds.Consumers;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Media.Features.Photos.Consumers;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Modules.Media.Services.ImageProcessing;
using VividImagination.Shared.Infrastructure.Mongo;
using Xunit;

namespace VividImagination.UnitTests;

public class ImageOptimizationTests
{
    [Fact]
    public async Task ImageProcessingService_Should_Generate_Three_WebP_Variants_And_Pixel_BlurHash()
    {
        // Arrange: Generate an in-memory test image (1200x800) with a color gradient
        using var testImage = new Image<Rgb24>(1200, 800);
        testImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < 800; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < 1200; x++)
                {
                    row[x] = new Rgb24((byte)(x % 256), (byte)(y % 256), 180);
                }
            }
        });

        using var memoryStream = new MemoryStream();
        await testImage.SaveAsPngAsync(memoryStream);
        memoryStream.Position = 0;

        var loggerMock = new Mock<ILogger<ImageSharpProcessingService>>();
        var service = new ImageSharpProcessingService(loggerMock.Object);

        // Act
        var result = await service.ProcessImageAsync(memoryStream);

        // Assert
        result.Should().NotBeNull();
        result.OriginalWidth.Should().Be(1200);
        result.OriginalHeight.Should().Be(800);
        result.BlurHash.Should().NotBeNullOrWhiteSpace();
        result.BlurHash.Length.Should().BeGreaterThan(10);

        // Verify variants
        result.Original.Should().NotBeNull();
        result.Original.ContentType.Should().Be("image/webp");
        result.Original.Bytes.Length.Should().BeGreaterThan(0);
        result.Original.Width.Should().Be(1200);

        result.Medium.Should().NotBeNull();
        result.Medium.ContentType.Should().Be("image/webp");
        result.Medium.Width.Should().Be(1080);
        result.Medium.Height.Should().Be(720); // 1200:800 ratio maintained
        result.Medium.Bytes.Length.Should().BeGreaterThan(0);

        result.Thumbnail.Should().NotBeNull();
        result.Thumbnail.ContentType.Should().Be("image/webp");
        result.Thumbnail.Width.Should().Be(256);
        result.Thumbnail.Height.Should().Be(171); // 1200:800 ratio maintained
        result.Thumbnail.Bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BlurHashGenerator_Should_Produce_Valid_BlurHash_From_Image_Pixels()
    {
        // Arrange
        using var testImage = new Image<Rgb24>(100, 100);
        testImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < 100; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < 100; x++)
                {
                    row[x] = new Rgb24(255, 128, 64);
                }
            }
        });

        // Act
        var hash = BlurHashGenerator.Encode(testImage, 4, 3);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        // 4*3 components: 1 (size) + 1 (max AC) + 4 (DC) + 2 * (12 - 1) (AC) = 28 characters
        hash.Length.Should().Be(28);
    }

    [Fact]
    public async Task PhotoOptimizationConsumer_Should_Process_Image_Store_Variants_And_Publish_PhotoOptimizedIntegrationEvent()
    {
        // Arrange
        var fileServiceManagerMock = new Mock<IFileServiceManager>();
        var imageProcessingServiceMock = new Mock<IImageProcessingService>();
        var fileRepositoryMock = new Mock<IMongoRepository<FileEntry, Guid>>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<PhotoOptimizationConsumer>>();

        var photoId = Guid.NewGuid();
        var fileEntry = new FileEntry
        {
            Id = photoId,
            Name = "sunset",
            FileName = "sunset.jpg",
            FileExtension = ".jpg",
            ContainerName = "photos",
            ContentType = "image/jpeg",
            Status = "Processing"
        };

        fileRepositoryMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileEntry);

        fileServiceManagerMock.Setup(f => f.ReadAsync(fileEntry, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xFF, 0xD8, 0xFF }); // Mock raw bytes

        var processedResult = new ProcessedImageResult(
            1920, 1080, "L6PZfSi_.AyE_3t7t7R**0o#DgR4",
            new ImageVariantResult(new byte[] { 1, 2 }, "original", 1920, 1080, 2),
            new ImageVariantResult(new byte[] { 3, 4 }, "medium", 1080, 608, 2),
            new ImageVariantResult(new byte[] { 5, 6 }, "thumbnail", 256, 144, 2));

        imageProcessingServiceMock.Setup(s => s.ProcessImageAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processedResult);

        var consumer = new PhotoOptimizationConsumer(
            fileServiceManagerMock.Object,
            imageProcessingServiceMock.Object,
            fileRepositoryMock.Object,
            publishEndpointMock.Object,
            loggerMock.Object);

        var consumeContextMock = new Mock<ConsumeContext<PhotoUploadedIntegrationEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(new PhotoUploadedIntegrationEvent(
            photoId, Guid.NewGuid(), "photos/sunset.jpg", "placeholder", "Sunset caption", DateTime.UtcNow));
        consumeContextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(consumeContextMock.Object);

        // Assert: 3 variants stored
        fileServiceManagerMock.Verify(f => f.CreateAsync(It.IsAny<IFileEntry>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Assert: FileEntry updated with Optimized status and BlurHash
        fileRepositoryMock.Verify(r => r.UpdateAsync(It.Is<FileEntry>(e =>
            e.Id == photoId &&
            e.Status == "Optimized" &&
            e.BlurHash == "L6PZfSi_.AyE_3t7t7R**0o#DgR4" &&
            e.Variants.Count == 3 &&
            e.Variants.ContainsKey("thumbnail") &&
            e.Variants.ContainsKey("medium") &&
            e.Variants.ContainsKey("original")), It.IsAny<CancellationToken>()), Times.Once);

        // Assert: PhotoOptimizedIntegrationEvent published
        publishEndpointMock.Verify(p => p.Publish(It.Is<PhotoOptimizedIntegrationEvent>(e =>
            e.PhotoId == photoId &&
            e.BlurHash == "L6PZfSi_.AyE_3t7t7R**0o#DgR4" &&
            e.Width == 1920 &&
            e.Height == 1080), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhotoOptimizedFeedConsumer_Should_Update_FeedItem_With_BlurHash_And_Medium_Key()
    {
        // Arrange
        var repositoryMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<PhotoOptimizedFeedConsumer>>();

        var photoId = Guid.NewGuid();
        var feedItem = new FeedItem
        {
            Id = photoId,
            PhotoId = photoId,
            StorageKey = "raw/photo.jpg",
            BlurHash = "oldPlaceholder",
            Caption = "Test photo"
        };

        repositoryMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItem);

        var consumer = new PhotoOptimizedFeedConsumer(
            repositoryMock.Object,
            mediatorMock.Object,
            loggerMock.Object);

        var consumeContextMock = new Mock<ConsumeContext<PhotoOptimizedIntegrationEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(new PhotoOptimizedIntegrationEvent(
            photoId,
            "LGF5?xYk^6#M@-5cSimple9F$M_Io",
            "photos/photo_original.webp",
            "photos/photo_medium.webp",
            "photos/photo_thumb.webp",
            1920,
            1080,
            50000,
            DateTime.UtcNow));
        consumeContextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(consumeContextMock.Object);

        // Assert: FeedItem updated with medium key and true blurhash
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<FeedItem>(f =>
            f.PhotoId == photoId &&
            f.StorageKey == "photos/photo_medium.webp" &&
            f.BlurHash == "LGF5?xYk^6#M@-5cSimple9F$M_Io"), It.IsAny<CancellationToken>()), Times.Once);

        // Assert: Feed cache invalidated
        mediatorMock.Verify(m => m.Send(It.IsAny<InvalidateFeedCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
