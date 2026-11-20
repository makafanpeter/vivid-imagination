using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Media.Contracts.Queries;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Moderation.Features.Reviews.Commands;

public class UpdateModerationVerdictCommandHandler(
    IMongoRepository<Review, Guid> repository,
    IPublishEndpoint publishEndpoint,
    IRequestClient<ValidatePhotoRequest> photoClient,
    ILogger<UpdateModerationVerdictCommandHandler> logger)
    : IRequestHandler<UpdateModerationVerdictCommand, Review>
{
    public async Task<Review> Handle(UpdateModerationVerdictCommand request, CancellationToken ct)
    {
        var photoResponse = await photoClient.GetResponse<PhotoValidationResponse>(
            new ValidatePhotoRequest(request.PhotoId), ct);

        if (!photoResponse.Message.Exists || photoResponse.Message.IsDeleted)
        {
            throw new NotFoundException($"Photo with ID '{request.PhotoId}' not found in media library.");
        }

        var review = await repository.FindByIdAsync(request.PhotoId, ct)
                     ?? throw new NotFoundException($"Moderation review for photo {request.PhotoId} not found");

        var isUnsafe = request.Verdict != "Approved";
        review.Verdict = request.Verdict;
        review.IsUnsafe = isUnsafe;
        review.BlurRequired = request.BlurRequired;
        if (!string.IsNullOrEmpty(request.Reason))
        {
            review.SafetyCategory = request.Reason;
        }
        review.ReviewedAt = DateTime.UtcNow;

        await repository.UpdateAsync(review, ct);

        logger.LogInformation("Moderator manually updated verdict for PhotoId: {PhotoId} to {Verdict}",
            request.PhotoId, request.Verdict);

        await publishEndpoint.Publish(new PhotoModeratedIntegrationEvent(
            review.PhotoId,
            review.IsUnsafe,
            review.BlurRequired,
            review.SafetyCategory,
            review.Verdict,
            review.MaxRiskScore,
            review.ReviewedAt), ct);

        return review;
    }
}
