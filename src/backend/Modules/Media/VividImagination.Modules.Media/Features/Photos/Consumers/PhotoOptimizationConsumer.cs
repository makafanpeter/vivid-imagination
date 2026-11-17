using MassTransit;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Modules.Media.Services.ImageProcessing;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Media.Features.Photos.Consumers;

public class PhotoOptimizationConsumer(
    IFileServiceManager fileServiceManager,
    IImageProcessingService imageProcessingService,
    IMongoRepository<FileEntry, Guid> fileRepository,
    IPublishEndpoint publishEndpoint,
    ILogger<PhotoOptimizationConsumer> logger)
    : IConsumer<PhotoUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PhotoUploadedIntegrationEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("PhotoOptimizationConsumer: Starting background optimization for PhotoId {PhotoId}", message.PhotoId);

        var fileEntry = await fileRepository.FindByIdAsync(message.PhotoId, context.CancellationToken);
        if (fileEntry == null)
        {
            logger.LogWarning("PhotoOptimizationConsumer: FileEntry {PhotoId} not found in database", message.PhotoId);
            return;
        }

        // Read raw uploaded file bytes
        var rawBytes = await fileServiceManager.ReadAsync(fileEntry, context.CancellationToken);
        if (rawBytes == null || rawBytes.Length == 0)
        {
            logger.LogWarning("PhotoOptimizationConsumer: File data for PhotoId {PhotoId} could not be retrieved from storage", message.PhotoId);
            return;
        }

        try
        {
            using var rawStream = new MemoryStream(rawBytes);
            var result = await imageProcessingService.ProcessImageAsync(rawStream, context.CancellationToken);

            var variants = new Dictionary<string, FileVariantInfo>();
            long totalSize = 0;

            // 1. Original WebP
            var originalModel = new FileEntryModel
            {
                Id = fileEntry.Id,
                ContainerName = fileEntry.ContainerName,
                FileName = $"{fileEntry.Name}_original.webp",
                FileExtension = "_original.webp",
                ContentType = "image/webp",
                Size = result.Original.ContentLength
            };
            using (var stream = new MemoryStream(result.Original.Bytes))
            {
                await fileServiceManager.CreateAsync(originalModel, stream, context.CancellationToken);
            }
            variants["original"] = new FileVariantInfo
            {
                VariantName = "original",
                StorageKey = originalModel.FileLocation ?? originalModel.FileName,
                Width = result.Original.Width,
                Height = result.Original.Height,
                Size = result.Original.ContentLength,
                ContentType = "image/webp"
            };
            totalSize += result.Original.ContentLength;

            // 2. Medium WebP (Max 1080px)
            var mediumModel = new FileEntryModel
            {
                Id = fileEntry.Id,
                ContainerName = fileEntry.ContainerName,
                FileName = $"{fileEntry.Name}_medium.webp",
                FileExtension = "_medium.webp",
                ContentType = "image/webp",
                Size = result.Medium.ContentLength
            };
            using (var stream = new MemoryStream(result.Medium.Bytes))
            {
                await fileServiceManager.CreateAsync(mediumModel, stream, context.CancellationToken);
            }
            variants["medium"] = new FileVariantInfo
            {
                VariantName = "medium",
                StorageKey = mediumModel.FileLocation ?? mediumModel.FileName,
                Width = result.Medium.Width,
                Height = result.Medium.Height,
                Size = result.Medium.ContentLength,
                ContentType = "image/webp"
            };
            totalSize += result.Medium.ContentLength;

            // 3. Thumbnail WebP (Max 256px)
            var thumbModel = new FileEntryModel
            {
                Id = fileEntry.Id,
                ContainerName = fileEntry.ContainerName,
                FileName = $"{fileEntry.Name}_thumb.webp",
                FileExtension = "_thumb.webp",
                ContentType = "image/webp",
                Size = result.Thumbnail.ContentLength
            };
            using (var stream = new MemoryStream(result.Thumbnail.Bytes))
            {
                await fileServiceManager.CreateAsync(thumbModel, stream, context.CancellationToken);
            }
            variants["thumbnail"] = new FileVariantInfo
            {
                VariantName = "thumbnail",
                StorageKey = thumbModel.FileLocation ?? thumbModel.FileName,
                Width = result.Thumbnail.Width,
                Height = result.Thumbnail.Height,
                Size = result.Thumbnail.ContentLength,
                ContentType = "image/webp"
            };
            totalSize += result.Thumbnail.ContentLength;

            // Update FileEntry in MongoDB
            fileEntry.Status = "Optimized";
            fileEntry.BlurHash = result.BlurHash;
            fileEntry.Width = result.OriginalWidth;
            fileEntry.Height = result.OriginalHeight;
            fileEntry.Variants = variants;

            await fileRepository.UpdateAsync(fileEntry, context.CancellationToken);

            logger.LogInformation("PhotoOptimizationConsumer: Successfully optimized PhotoId {PhotoId} with BlurHash {BlurHash}", message.PhotoId, result.BlurHash);

            // Publish PhotoOptimizedIntegrationEvent to notify FeedModule and downstream consumers
            await publishEndpoint.Publish(new PhotoOptimizedIntegrationEvent(
                fileEntry.Id,
                result.BlurHash,
                variants["original"].StorageKey,
                variants["medium"].StorageKey,
                variants["thumbnail"].StorageKey,
                result.OriginalWidth,
                result.OriginalHeight,
                totalSize,
                DateTime.UtcNow), context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PhotoOptimizationConsumer: Error optimizing photo {PhotoId}", message.PhotoId);
            fileEntry.Status = "Failed";
            await fileRepository.UpdateAsync(fileEntry, context.CancellationToken);
            throw; // Re-throw so MassTransit retry policy handles transient errors
        }
    }
}
