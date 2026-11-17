using MediatR;
using VividImagination.Modules.Moderation.Persistence.Documents;

namespace VividImagination.Modules.Moderation.Features.Reviews.Commands;

public record UpdateModerationVerdictCommand(
    Guid PhotoId,
    string Verdict,
    string? Reason,
    bool BlurRequired) : IRequest<Review>;
