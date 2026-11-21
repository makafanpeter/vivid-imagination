namespace VividImagination.Modules.Feed.Ranking;

public static class FeedRankingCalculator
{
    /// <summary>
    /// Computes the dynamic ranking score:
    /// Score = (Upvotes - Downvotes) / (AgeInHours + 2)^1.5
    /// </summary>
    public static double CalculateScore(int upvotes, int downvotes, DateTimeOffset uploadedAt)
    {
        double ageInHours = Math.Max(0.0, (DateTime.UtcNow - uploadedAt).TotalHours);
        int netVotes = upvotes - downvotes;
        return netVotes / Math.Pow(ageInHours + 2.0, 1.5);
    }
}
