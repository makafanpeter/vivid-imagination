using MediatR;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Social.Features.Reports.Dtos;
using VividImagination.Modules.Social.Persistence;

namespace VividImagination.Modules.Social.Features.Reports.Queries;

public class GetPhotoReportQueryHandler(SocialDbContext db) : IRequestHandler<GetPhotoReportQuery, ReportResult>
{
    public async Task<ReportResult> Handle(GetPhotoReportQuery request, CancellationToken cancellationToken)
    {
        var totalReports = await db.Reports.CountAsync(r => r.PhotoId == request.PhotoId, cancellationToken);
        return new ReportResult(request.PhotoId, totalReports);
    }
}
