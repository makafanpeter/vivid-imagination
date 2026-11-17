namespace VividImagination.Modules.Moderation.Features.Reviews.Dtos;

public record UpdateModerationVerdictRequest(
    string Verdict,
    string? Reason,
    bool BlurRequired = false);
