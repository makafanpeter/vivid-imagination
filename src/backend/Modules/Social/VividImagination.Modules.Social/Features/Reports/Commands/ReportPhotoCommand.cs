using MediatR;
using VividImagination.Modules.Social.Features.Reports.Dtos;

namespace VividImagination.Modules.Social.Features.Reports.Commands;

public record ReportPhotoCommand(Guid PhotoId, string? Reason) : IRequest<ReportResult>;
