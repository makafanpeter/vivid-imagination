using VividImagination.Modules.Feed.Persistence.Entities;

namespace VividImagination.Modules.Feed.Ranking;

public record PersonalizedScoreBreakdown(
    double Recency,
    double Quality,
    double TagAffinity,
    double CreatorAffinity,
    double NegativeAffinity,
    double ExplorationBoost,
    double CompositeScore);

public static class PersonalizedRankingCalculator
{
    public const double WeightRecency = 0.25;
    public const double WeightQuality = 0.20;
    public const double WeightTagAffinity = 0.30;
    public const double WeightCreatorAffinity = 0.15;
    public const double WeightNegativeAffinity = -0.15;
    public const double WeightExploration = 0.10;

    public static double CalculateRecency(DateTime uploadedAt, DateTime utcNow)
    {
        var ageInHours = Math.Max(0.0, (utcNow - uploadedAt).TotalHours);
        return 1.0 / (1.0 + (ageInHours / 24.0));
    }

    public static double CalculateQuality(int upvotes, int downvotes)
    {
        var positiveUpvotes = Math.Max(0, upvotes);
        return (double)positiveUpvotes / (positiveUpvotes + Math.Max(0, downvotes) + 1.0);
    }

    public static double CalculateTagAffinity(IReadOnlyList<string> photoTags, UserInterestProfile? profile)
    {
        if (profile == null || profile.TagAffinities.Count == 0 || photoTags.Count == 0)
            return 0.0;

        double sum = 0.0;
        int matchCount = 0;

        foreach (var tag in photoTags)
        {
            if (profile.TagAffinities.TryGetValue(tag, out var affinity) && affinity > 0)
            {
                sum += affinity;
                matchCount++;
            }
        }

        if (matchCount == 0)
            return 0.0;

        var avg = sum / matchCount;
        return avg / (avg + 1.0);
    }

    public static double CalculateCreatorAffinity(Guid creatorId, UserInterestProfile? profile)
    {
        if (profile == null || profile.CreatorAffinities.Count == 0)
            return 0.0;

        var key = creatorId.ToString();
        if (profile.CreatorAffinities.TryGetValue(key, out var affinity) && affinity > 0)
        {
            return affinity / (affinity + 1.0);
        }

        return 0.0;
    }

    public static double CalculateNegativeAffinity(Guid creatorId, IReadOnlyList<string> photoTags, UserInterestProfile? profile)
    {
        if (profile == null)
            return 0.0;

        if (profile.NegativeCreators.Contains(creatorId))
            return 1.0;

        if (profile.NegativeTags.Count > 0 && photoTags.Any(t => profile.NegativeTags.Contains(t, StringComparer.OrdinalIgnoreCase)))
            return 1.0;

        return 0.0;
    }

    public static double CalculateExplorationBoost(int upvotes, int downvotes)
    {
        var totalVotes = Math.Max(0, upvotes) + Math.Max(0, downvotes);
        if (totalVotes < 5)
        {
            return 1.0 - (totalVotes / 5.0);
        }

        return 0.0;
    }

    public static double CalculateCompositeScore(double r, double q, double t, double c, double n, double e)
    {
        var score = (WeightRecency * r)
                  + (WeightQuality * q)
                  + (WeightTagAffinity * t)
                  + (WeightCreatorAffinity * c)
                  + (WeightNegativeAffinity * n)
                  + (WeightExploration * e);

        return Math.Round(score, 6);
    }

    public static PersonalizedScoreBreakdown ComputeBreakdown(FeedItem item, UserInterestProfile? profile, DateTime utcNow)
    {
        var tags = TagExtractor.ExtractTags(item.Caption);
        var r = CalculateRecency(item.UploadedAt, utcNow);
        var q = CalculateQuality(item.Upvotes, item.Downvotes);
        var t = CalculateTagAffinity(tags, profile);
        var c = CalculateCreatorAffinity(item.UserId, profile);
        var n = CalculateNegativeAffinity(item.UserId, tags, profile);
        var e = CalculateExplorationBoost(item.Upvotes, item.Downvotes);
        var composite = CalculateCompositeScore(r, q, t, c, n, e);

        return new PersonalizedScoreBreakdown(r, q, t, c, n, e, composite);
    }

    public static double CalculateScore(FeedItem item, UserInterestProfile? profile, DateTime utcNow)
    {
        return ComputeBreakdown(item, profile, utcNow).CompositeScore;
    }
}
