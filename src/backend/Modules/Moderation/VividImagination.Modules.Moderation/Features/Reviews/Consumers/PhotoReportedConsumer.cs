using MassTransit;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Moderation.Features.Reviews.Consumers;

public class PhotoReportedConsumer(
    IMongoRepository<ModerationReview, Guid> repository,
    IPublishEndpoint publishEndpoint,
    ILogger<PhotoReportedConsumer> logger)
    : IConsumer<PhotoReportedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PhotoReportedIntegrationEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Processing Community Report for PhotoId: {PhotoId}, TotalReports: {TotalReports}", message.PhotoId, message.TotalReports);

        var review = await repository.FindByIdAsync(message.PhotoId, context.CancellationToken);
        var isNew = review == null;

        if (review == null)
        {
            review = new ModerationReview
            {
                Id = message.PhotoId,
                PhotoId = message.PhotoId,
                ReviewedAt = DateTime.UtcNow
            };
        }

        review.ReportCount = message.TotalReports;

        // FR-MOD-06 (The 2-Flag Rule): At >= 2 reports, automatically tag unsafe and require blur
        if (message.TotalReports >= 2 && review.Verdict != "Rejected")
        {
            review.IsUnsafe = true;
            review.BlurRequired = true;
            review.Verdict = "TaggedUnsafe";
            review.SafetyCategory = "Community Flagged (2-Flag Rule)";

            if (isNew)
            {
                await repository.AddAsync(review, context.CancellationToken);
            }
            else
            {
                await repository.UpdateAsync(review, context.CancellationToken);
            }

            logger.LogWarning("Photo {PhotoId} triggered the 2-Flag Community Moderation Rule! Tagging as Unsafe with UI blur shield", message.PhotoId);

            await publishEndpoint.Publish(new PhotoModeratedIntegrationEvent(
                review.PhotoId,
                review.IsUnsafe,
                review.BlurRequired,
                review.SafetyCategory,
                review.Verdict,
                review.MaxRiskScore,
                DateTime.UtcNow), context.CancellationToken);
        }
        else
        {
            if (isNew)
            {
                await repository.AddAsync(review, context.CancellationToken);
            }
            else
            {
                await repository.UpdateAsync(review, context.CancellationToken);
            }
        }
    }
}
