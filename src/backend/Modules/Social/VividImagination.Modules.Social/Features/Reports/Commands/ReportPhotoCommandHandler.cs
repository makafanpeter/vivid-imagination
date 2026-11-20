using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Media.Contracts.Queries;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Modules.Social.Features.Reports.Dtos;
using VividImagination.Modules.Social.Persistence;
using VividImagination.Modules.Social.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Social.Features.Reports.Commands;

public class ReportPhotoCommandHandler(
    SocialDbContext db,
    IPublishEndpoint publisher,
    IRequestClient<ValidatePhotoRequest> photoClient,
    ICurrentUser currentUser)
    : IRequestHandler<ReportPhotoCommand, ReportResult>
{
    public async Task<ReportResult> Handle(ReportPhotoCommand request, CancellationToken ct)
    {
        if (currentUser.CurrentUser == null)
        {
            throw new UnAuthorizedException("You are not authorized to report photo");
        }

        var photoResponse = await photoClient.GetResponse<PhotoValidationResponse>(
            new ValidatePhotoRequest(request.PhotoId), ct);

        if (!photoResponse.Message.Exists || photoResponse.Message.IsDeleted)
        {
            throw new NotFoundException($"Photo with ID '{request.PhotoId}' not found.");
        }

        var userId = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized to report photo");
        var alreadyReported = await db.Reports.AnyAsync(
            r => r.PhotoId == request.PhotoId && r.ReporterUserId == userId, ct);

        if (!alreadyReported)
        {
            db.Reports.Add(new PhotoReport
            {
                Id = Guid.NewGuid(),
                PhotoId = request.PhotoId,
                ReporterUserId = userId,
                Reason = request.Reason ?? (request.ReasonType != ReportReasonType.Other ? request.ReasonType.ToString() : "Inappropriate content"),
                ReportedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        var totalReports = await db.Reports.CountAsync(r => r.PhotoId == request.PhotoId, ct);

        await publisher.Publish(new PhotoReportedIntegrationEvent(
            request.PhotoId,
            userId,
            request.Reason,
            totalReports,
            DateTime.UtcNow), ct);

        return new ReportResult(request.PhotoId, totalReports);
    }
}
