using MediatR;
using Microsoft.AspNetCore.Mvc;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public sealed record GetPersonalizedFeedQuery(
    int Page = 1,
    int PageSize = 20,
    SafeBrowsingMode? safeBrowsingMode = null,
    IReadOnlyList<Guid>? ExcludedPhotoIds = null) : IRequest<IReadOnlyList<FeedItemDto>>;
