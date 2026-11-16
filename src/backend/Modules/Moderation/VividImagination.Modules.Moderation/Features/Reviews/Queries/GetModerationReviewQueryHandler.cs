using MediatR;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Moderation.Features.Reviews.Queries;

public sealed class GetModerationReviewQueryHandler(IMongoRepository<ModerationReview, Guid> repository)
    : IRequestHandler<GetModerationReviewQuery, ModerationReview>
{
    public async Task<ModerationReview> Handle(
        GetModerationReviewQuery request,
        CancellationToken cancellationToken)
    {
        var result = await repository.FindByIdAsync(request.PhotoId, cancellationToken);

        return result == null ? throw new NotFoundException("The requested moderation review was not found") : result;
    }
}