using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Social.Contracts.Events;

public record PhotoReportedIntegrationEvent(
    Guid PhotoId,
    long ReporterUserId,
    string? Reason,
    int TotalReports,
    DateTime ReportedAt) : IntegrationEvent(PhotoId, ReportedAt);
