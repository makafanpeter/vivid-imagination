using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Modules.Moderation.Services;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Moderation.Features.Reviews.Consumers;

public class PhotoUploadedConsumer(
    IAmazonRekognitionService rekognitionService,
    IMongoRepository<Review, Guid> repository,
    IPublishEndpoint publishEndpoint,
    ILogger<PhotoUploadedConsumer> logger , IOptions<ApplicationSettings> options)
    : IConsumer<PhotoUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PhotoUploadedIntegrationEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Processing Amazon Rekognition inspection for PhotoId: {PhotoId}, Key: {StorageKey}", message.PhotoId, message.StorageKey);

        var bucketName = options.Value.Storage.S3.BucketName;
        var result = await rekognitionService.InspectPhotoAsync(bucketName , message.StorageKey, context.CancellationToken);

        var review = new Review
        {
            Id = message.PhotoId,
            PhotoId = message.PhotoId,
            MaxRiskScore = result.MaxRiskScore,
            AdultScore = result.AdultScore,
            ViolenceScore = result.ViolenceScore,
            HateScore = result.HateScore,
            SafetyCategory = result.SafetyCategory,
            Verdict = result.Verdict,
            IsUnsafe = result.IsUnsafe,
            BlurRequired = result.BlurRequired,
            ReportCount = 0,
            Labels = result.Labels,
            ReviewedAt = DateTime.UtcNow
        };

        await repository.AddAsync(review, context.CancellationToken);

        // Publish moderation verdict for Feed and other modules
        await publishEndpoint.Publish(new PhotoModeratedIntegrationEvent(
            review.PhotoId,
            review.IsUnsafe,
            review.BlurRequired,
            review.SafetyCategory,
            review.Verdict,
            review.MaxRiskScore,
            review.ReviewedAt), context.CancellationToken);

        logger.LogInformation("Completed AI Moderation for PhotoId: {PhotoId}. Verdict: {Verdict}, BlurRequired: {BlurRequired}",
            review.PhotoId, review.Verdict, review.BlurRequired);
    }
}
