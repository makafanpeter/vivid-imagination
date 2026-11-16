using MediatR;
using VividImagination.Modules.Moderation.Persistence.Documents;

namespace VividImagination.Modules.Moderation.Features.Reviews.Queries;

public sealed record GetModerationReviewQuery(Guid PhotoId): IRequest<ModerationReview>;