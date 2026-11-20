using VividImagination.Modules.Feed.Persistence.Entities;

namespace VividImagination.Modules.Feed.Ranking;

public interface ICandidateGenerator
{
    Task<IReadOnlyList<FeedItem>> GenerateCandidatesAsync(
        UserInterestProfile? profile,
        int targetCount = 300,
        CancellationToken ct = default);
}
