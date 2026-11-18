namespace VividImagination.Modules.Social.Features.Reports.Dtos;

public record ReportRequest(string? Reason, ReportReasonType ReasonType = ReportReasonType.Other);
