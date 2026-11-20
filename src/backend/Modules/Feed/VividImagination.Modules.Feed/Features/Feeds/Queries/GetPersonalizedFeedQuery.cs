using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public sealed record GetPersonalizedFeedQuery(
    int Page = 1,
    int PageSize = 20,
    SafeBrowsingMode? SafeBrowsingMode = null,
    IReadOnlyList<Guid>? ExcludedPhotoIds = null,
    Guid UserId = default,
    bool IsPersonalisationEnabled = true) : IRequest<IReadOnlyList<FeedItemDto>>;
