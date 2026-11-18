using MediatR;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public class GetPersonalizedFeedQueryHandler : IRequestHandler<GetPersonalizedFeedQuery, IReadOnlyList<FeedItemDto>>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;

    public GetPersonalizedFeedQueryHandler(IMongoRepository<FeedItem, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FeedItemDto>> Handle(GetPersonalizedFeedQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, request.Page - 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var hideSensitive = string.Equals(request.SafeBrowsingMode, "HideSensitive", StringComparison.OrdinalIgnoreCase);

        var sort = Builders<FeedItem>.Sort
            .Descending(x => x.Score)
            .Descending(x => x.UploadedAt);

        var pagedItems = await _repository.FindPagedAsync(
            x => x.Verdict != "Rejected" && (!hideSensitive || (!x.IsUnsafe && !x.BlurRequired)),
            pageIndex,
            pageSize,
            sort,
            cancellationToken);

        var items = pagedItems.AsEnumerable();

        if (request.ExcludedPhotoIds != null && request.ExcludedPhotoIds.Count > 0)
        {
            var excludedSet = request.ExcludedPhotoIds.ToHashSet();
            items = items.Where(x => !excludedSet.Contains(x.PhotoId));
        }

        return items.Select(i => new FeedItemDto(
            i.Id,
            i.PhotoId,
            i.UserId,
            i.StorageKey,
            i.BlurHash,
            i.Caption,
            i.Upvotes,
            i.Downvotes,
            i.Score,
            i.IsUnsafe,
            i.BlurRequired,
            i.SafetyCategory,
            i.BlurRequired ? $"Sensitive Content: {i.SafetyCategory ?? "Content Flagged"}" : null,
            i.UploadedAt
        )).ToList();
    }
}
