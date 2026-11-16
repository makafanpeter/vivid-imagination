using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Moderation.Contracts.Events;

public record PhotoModeratedIntegrationEvent(
    Guid PhotoId,
    bool IsUnsafe,
    bool BlurRequired,
    string? SafetyCategory,
    string Verdict,
    double RiskScore,
    DateTime ModeratedAt) : IntegrationEvent(PhotoId, ModeratedAt);
