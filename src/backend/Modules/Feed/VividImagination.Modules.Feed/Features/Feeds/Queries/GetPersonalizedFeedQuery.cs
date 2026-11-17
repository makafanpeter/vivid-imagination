using MediatR;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public sealed record GetPersonalizedFeedQuery(
    int Page = 1,
    int PageSize = 20,
    string? SafeBrowsingMode = null,
    IReadOnlyList<Guid>? ExcludedPhotoIds = null) : IRequest<IReadOnlyList<FeedItemDto>>;
