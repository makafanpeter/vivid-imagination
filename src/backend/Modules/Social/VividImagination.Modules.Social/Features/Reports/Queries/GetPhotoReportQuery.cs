using MediatR;
using VividImagination.Modules.Social.Features.Reports.Dtos;

namespace VividImagination.Modules.Social.Features.Reports.Queries;

public record GetPhotoReportQuery(Guid PhotoId) : IRequest<ReportResult>;
