using MediatR;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Caching;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Features.Feeds.Queries;

public class GetPersonalizedFeedQueryHandler : IRequestHandler<GetPersonalizedFeedQuery, IReadOnlyList<FeedItemDto>>
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IMongoRepository<UserInterestProfile, Guid>? _profileRepository;
    private readonly ICandidateGenerator? _candidateGenerator;
    private readonly IMediator? _mediator;
    private readonly IStaticCacheManager? _cacheManager;

    public GetPersonalizedFeedQueryHandler(
        IMongoRepository<FeedItem, Guid> repository,
        IMongoRepository<UserInterestProfile, Guid>? profileRepository = null,
        ICandidateGenerator? candidateGenerator = null,
        IMediator? mediator = null,
        IStaticCacheManager? cacheManager = null)
    {
        _repository = repository;
        _profileRepository = profileRepository;
        _candidateGenerator = candidateGenerator;
        _mediator = mediator;
        _cacheManager = cacheManager;
    }

    public async Task<IReadOnlyList<FeedItemDto>> Handle(GetPersonalizedFeedQuery request, CancellationToken cancellationToken)
    {
        // 1. Personalisation toggle check: fallback to global feed if disabled
        if (!request.IsPersonalisationEnabled)
        {
            if (_mediator != null)
            {
                return await _mediator.Send(new GetFeedQuery(request.Page, request.PageSize, request.SafeBrowsingMode), cancellationToken);
            }
            return await FallbackGlobalQueryAsync(request, cancellationToken);
        }

        // 2. Cold-start check: user with < 3 interactions receives global feed fallback
        UserInterestProfile? profile = null;
        if (request.UserId != Guid.Empty && _profileRepository != null)
        {
            profile = await _profileRepository.FindOneAsync(x => x.UserId == request.UserId, cancellationToken);
            if (profile == null || profile.TotalInteractions < 3)
            {
                if (_mediator != null)
                {
                    return await _mediator.Send(new GetFeedQuery(request.Page, request.PageSize, request.SafeBrowsingMode), cancellationToken);
                }
                return await FallbackGlobalQueryAsync(request, cancellationToken);
            }
        }

        // 3. Redis cache check
        var pageIndex = Math.Max(0, request.Page - 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        if (_cacheManager != null && request.UserId != Guid.Empty)
        {
            var cacheKey = FeedCacheDefaults.PersonalizedFeedCacheKey;
            var cached = await _cacheManager.GetAsync<IReadOnlyList<FeedItemDto>?>(
                cacheKey,
                (Func<IReadOnlyList<FeedItemDto>?>)(() => null));

            if (cached != null && cached.Count > 0)
            {
                return cached;
            }
        }

        // 4. Candidate Generation
        IReadOnlyList<FeedItem> candidates;
        if (_candidateGenerator != null)
        {
            candidates = await _candidateGenerator.GenerateCandidatesAsync(profile, 300, cancellationToken);
        }
        else
        {
            var paged = await _repository.FindPagedAsync(
                x => x.Verdict != "Rejected",
                0,
                100,
                Builders<FeedItem>.Sort.Descending(x => x.Score),
                cancellationToken);
            candidates = paged.ToList();
        }

        // 5. Diversity Filtering and Ranking
        var diversified = DiversityFilter.FilterAndDiversify(
            candidates,
            profile,
            request.SafeBrowsingMode,
            request.ExcludedPhotoIds,
            DateTime.UtcNow);

        // 6. Pagination Slice
        var pagedItems = diversified.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        // 7. Map to DTO
        var dtoList = pagedItems.Select(i =>
        {
            var blurRequired = request.SafeBrowsingMode == SafeBrowsingMode.AlwaysShowSensitive
                ? false
                : i.BlurRequired;

            var warning = blurRequired
                ? $"Sensitive Content: {i.SafetyCategory ?? "Content Flagged"}"
                : null;

            return new FeedItemDto(
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
                blurRequired,
                i.SafetyCategory,
                warning,
                i.CreatedOn
            );
        }).ToList();

        // 8. Cache result
        if (_cacheManager != null && request.UserId != Guid.Empty && dtoList.Count > 0)
        {
            try
            {
                await _cacheManager.SetAsync(FeedCacheDefaults.PersonalizedFeedCacheKey, dtoList);
            }
            catch
            {
                // Cache storage failure should not prevent feed delivery
            }
        }

        return dtoList;
    }

    private async Task<IReadOnlyList<FeedItemDto>> FallbackGlobalQueryAsync(
        GetPersonalizedFeedQuery request,
        CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, request.Page - 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var hideSensitive = request.SafeBrowsingMode == SafeBrowsingMode.HideSensitive;

        var sort = Builders<FeedItem>.Sort
            .Descending(x => x.Score)
            .Descending(x => x.CreatedOn);

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
            i.CreatedOn
        )).ToList();
    }
}
