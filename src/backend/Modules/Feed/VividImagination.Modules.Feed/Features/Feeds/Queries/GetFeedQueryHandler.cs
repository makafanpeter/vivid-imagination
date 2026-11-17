using MediatR;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemDto>>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;

    public GetFeedQueryHandler(IMongoRepository<FeedItem, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FeedItemDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, request.Page - 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var sort = Builders<FeedItem>.Sort
            .Descending(x => x.Score)
            .Descending(x => x.UploadedAt);

        var pagedItems = await _repository.FindPagedAsync(
            x => x.Verdict != "Rejected",
            pageIndex,
            pageSize,
            sort,
            cancellationToken);

        return pagedItems.Select(i => new FeedItemDto(
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
