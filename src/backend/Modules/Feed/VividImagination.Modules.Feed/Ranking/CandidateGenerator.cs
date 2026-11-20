using MongoDB.Driver;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Ranking;

public class CandidateGenerator : ICandidateGenerator
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;

    public CandidateGenerator(IMongoRepository<FeedItem, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FeedItem>> GenerateCandidatesAsync(
        UserInterestProfile? profile,
        int targetCount = 300,
        CancellationToken ct = default)
    {
        var candidatePool = new Dictionary<Guid, FeedItem>();
        var sliceSize = Math.Max(20, targetCount / 5);

        // Slice 1: Recent uploads
        var recentItems = await _repository.FindPagedAsync(
            x => x.Verdict != "Rejected",
            0,
            sliceSize,
            Builders<FeedItem>.Sort.Descending(x => x.UploadedAt),
            ct);

        foreach (var item in recentItems)
        {
            candidatePool.TryAdd(item.PhotoId, item);
        }

        // Slice 2: Trending / Top Decay-Ranked
        var trendingItems = await _repository.FindPagedAsync(
            x => x.Verdict != "Rejected",
            0,
            sliceSize,
            Builders<FeedItem>.Sort.Descending(x => x.Score),
            ct);

        foreach (var item in trendingItems)
        {
            candidatePool.TryAdd(item.PhotoId, item);
        }

        // Slice 3: High Creator Affinity
        if (profile != null && profile.CreatorAffinities.Count > 0)
        {
            var topCreatorIds = profile.CreatorAffinities
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => Guid.TryParse(kv.Key, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

            if (topCreatorIds.Count > 0)
            {
                var creatorItems = await _repository.FindPagedAsync(
                    x => x.Verdict != "Rejected" && topCreatorIds.Contains(x.UserId),
                    0,
                    sliceSize,
                    Builders<FeedItem>.Sort.Descending(x => x.Score),
                    ct);

                foreach (var item in creatorItems)
                {
                    candidatePool.TryAdd(item.PhotoId, item);
                }
            }
        }

        // Slice 4: Preferred Tag Matches
        if (profile != null && profile.TagAffinities.Count > 0)
        {
            var topTags = profile.TagAffinities
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => kv.Key.ToLowerInvariant())
                .ToList();

            var extraPool = await _repository.FindPagedAsync(
                x => x.Verdict != "Rejected",
                0,
                sliceSize * 2,
                Builders<FeedItem>.Sort.Descending(x => x.UploadedAt),
                ct);

            foreach (var item in extraPool)
            {
                var itemTags = TagExtractor.ExtractTags(item.Caption);
                if (itemTags.Any(t => topTags.Contains(t.ToLowerInvariant())))
                {
                    candidatePool.TryAdd(item.PhotoId, item);
                }
            }
        }

        // Slice 5: Exploration candidates (low total votes / fresh exposure)
        var explorationItems = await _repository.FindPagedAsync(
            x => x.Verdict != "Rejected" && (x.Upvotes + x.Downvotes) < 5,
            0,
            sliceSize,
            Builders<FeedItem>.Sort.Descending(x => x.UploadedAt),
            ct);

        foreach (var item in explorationItems)
        {
            candidatePool.TryAdd(item.PhotoId, item);
        }

        return candidatePool.Values.Take(targetCount).ToList();
    }
}
