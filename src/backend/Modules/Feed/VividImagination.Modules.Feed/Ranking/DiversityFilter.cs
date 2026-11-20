using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Feed.Ranking;

public static class DiversityFilter
{
    public const int MaxConsecutivePerCreator = 2;
    public const double ExplorationRatio = 0.15; // 15% exploration target

    public static IReadOnlyList<FeedItem> FilterAndDiversify(
        IReadOnlyList<FeedItem> candidates,
        UserInterestProfile? profile,
        SafeBrowsingMode? safeBrowsingMode,
        IReadOnlyList<Guid>? excludedPhotoIds,
        DateTime utcNow)
    {
        if (candidates == null || candidates.Count == 0)
            return [];

        var excludedSet = excludedPhotoIds != null && excludedPhotoIds.Count > 0
            ? excludedPhotoIds.ToHashSet()
            : null;

        var downvotedSet = profile?.DownvotedPhotoIds != null && profile.DownvotedPhotoIds.Count > 0
            ? profile.DownvotedPhotoIds.ToHashSet()
            : null;

        var hiddenSet = profile?.HiddenPhotoIds != null && profile.HiddenPhotoIds.Count > 0
            ? profile.HiddenPhotoIds.ToHashSet()
            : null;

        var recentlyViewedSet = profile?.RecentlyViewedPhotoIds != null && profile.RecentlyViewedPhotoIds.Count > 0
            ? profile.RecentlyViewedPhotoIds.ToHashSet()
            : null;

        var hideSensitive = safeBrowsingMode == SafeBrowsingMode.HideSensitive;

        // 1. Filter ineligible items
        var eligible = new List<FeedItem>();
        foreach (var item in candidates)
        {
            if (string.Equals(item.Verdict, "Rejected", StringComparison.OrdinalIgnoreCase))
                continue;

            if (downvotedSet != null && downvotedSet.Contains(item.PhotoId))
                continue;

            if (hiddenSet != null && hiddenSet.Contains(item.PhotoId))
                continue;

            if (excludedSet != null && excludedSet.Contains(item.PhotoId))
                continue;

            if (hideSensitive && (item.BlurRequired || item.IsUnsafe))
                continue;

            eligible.Add(item);
        }

        if (eligible.Count == 0)
            return [];

        // 2. Score items and separate into main vs exploration
        var scoredMain = new List<(FeedItem Item, double Score)>();
        var scoredExploration = new List<(FeedItem Item, double Score)>();

        foreach (var item in eligible)
        {
            var breakdown = PersonalizedRankingCalculator.ComputeBreakdown(item, profile, utcNow);
            var finalScore = breakdown.CompositeScore;

            // Demote recently viewed photos
            if (recentlyViewedSet != null && recentlyViewedSet.Contains(item.PhotoId))
            {
                finalScore -= 0.20;
            }

            if (breakdown.ExplorationBoost >= 0.4)
            {
                scoredExploration.Add((item, finalScore));
            }
            else
            {
                scoredMain.Add((item, finalScore));
            }
        }

        scoredMain.Sort((a, b) => b.Score.CompareTo(a.Score));
        scoredExploration.Sort((a, b) => b.Score.CompareTo(a.Score));

        var mainQueue = new List<FeedItem>(scoredMain.Select(x => x.Item));
        var explorationQueue = new List<FeedItem>(scoredExploration.Select(x => x.Item));

        // 3. Interleave exploration items and apply max consecutive creator constraint
        var result = new List<FeedItem>(eligible.Count);
        int explorationInterval = (int)Math.Round(1.0 / ExplorationRatio); // e.g. every ~7th item

        while (mainQueue.Count > 0 || explorationQueue.Count > 0)
        {
            bool takeExploration = explorationQueue.Count > 0 &&
                                  (result.Count > 0 && (result.Count + 1) % explorationInterval == 0 || mainQueue.Count == 0);

            var primarySource = takeExploration ? explorationQueue : mainQueue;
            var secondarySource = takeExploration ? mainQueue : explorationQueue;

            var chosen = PickNextDiverseItem(result, primarySource);
            if (chosen == null && secondarySource.Count > 0)
            {
                chosen = PickNextDiverseItem(result, secondarySource);
            }

            // If diversity constraint cannot be met with any available item, fall back to whatever is left
            if (chosen == null)
            {
                if (primarySource.Count > 0)
                {
                    chosen = primarySource[0];
                    primarySource.RemoveAt(0);
                }
                else if (secondarySource.Count > 0)
                {
                    chosen = secondarySource[0];
                    secondarySource.RemoveAt(0);
                }
            }

            if (chosen != null)
            {
                result.Add(chosen);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private static FeedItem? PickNextDiverseItem(List<FeedItem> currentResult, List<FeedItem> candidates)
    {
        if (candidates.Count == 0)
            return null;

        if (currentResult.Count < MaxConsecutivePerCreator)
        {
            var item = candidates[0];
            candidates.RemoveAt(0);
            return item;
        }

        // Check if the last MaxConsecutivePerCreator items are from the same creator
        var lastCreator = currentResult[^1].UserId;
        bool isStreak = true;
        for (int i = 1; i <= MaxConsecutivePerCreator; i++)
        {
            if (currentResult[^i].UserId != lastCreator)
            {
                isStreak = false;
                break;
            }
        }

        if (!isStreak)
        {
            var item = candidates[0];
            candidates.RemoveAt(0);
            return item;
        }

        // Streak detected: find the first candidate with a different creator
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].UserId != lastCreator)
            {
                var item = candidates[i];
                candidates.RemoveAt(i);
                return item;
            }
        }

        return null;
    }
}
