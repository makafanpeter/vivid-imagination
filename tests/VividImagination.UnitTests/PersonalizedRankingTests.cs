using FluentAssertions;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using Xunit;

namespace VividImagination.UnitTests;

public class PersonalizedRankingTests
{
    [Fact]
    public void TagExtractor_ShouldExtractAndNormalizeHashtags()
    {
        var caption = "Golden hour by the sea #Nature #Sunset #ocean_view! Having coffee #coffee";
        var tags = TagExtractor.ExtractTags(caption);

        tags.Should().Contain(["nature", "sunset", "ocean_view", "coffee"]);
        tags.Should().OnlyContain(t => t == t.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TagExtractor_ShouldReturnFallbackTags_WhenCaptionIsEmpty(string? caption)
    {
        var tags = TagExtractor.ExtractTags(caption);
        tags.Should().NotBeEmpty();
        tags.Should().Contain("vivid");
    }

    [Fact]
    public void PersonalizedRankingCalculator_Recency_ShouldNormalizeBetweenZeroAndOne()
    {
        var now = DateTime.UtcNow;
        var justNow = PersonalizedRankingCalculator.CalculateRecency(now, now);
        var oneDayAgo = PersonalizedRankingCalculator.CalculateRecency(now.AddHours(-24), now);
        var threeDaysAgo = PersonalizedRankingCalculator.CalculateRecency(now.AddHours(-72), now);

        justNow.Should().BeApproximately(1.0, 0.001);
        oneDayAgo.Should().BeApproximately(0.5, 0.001);
        threeDaysAgo.Should().BeApproximately(0.25, 0.001);

        justNow.Should().BeInRange(0.0, 1.0);
        oneDayAgo.Should().BeInRange(0.0, 1.0);
        threeDaysAgo.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_Quality_ShouldNormalizeBetweenZeroAndOne()
    {
        var highQuality = PersonalizedRankingCalculator.CalculateQuality(100, 5);
        var neutral = PersonalizedRankingCalculator.CalculateQuality(0, 0);
        var poorQuality = PersonalizedRankingCalculator.CalculateQuality(0, 50);

        highQuality.Should().BeInRange(0.9, 1.0);
        neutral.Should().Be(0.0);
        poorQuality.Should().Be(0.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_TagAffinity_ShouldNormalizeBetweenZeroAndOne()
    {
        var profile = new UserInterestProfile
        {
            TagAffinities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["nature"] = 4.0,
                ["sunset"] = 2.0
            }
        };

        var affinity = PersonalizedRankingCalculator.CalculateTagAffinity(["nature", "sunset"], profile);
        affinity.Should().BeInRange(0.0, 1.0);
        // avg = 3.0, affinity = 3.0 / 4.0 = 0.75
        affinity.Should().BeApproximately(0.75, 0.01);

        var noMatchAffinity = PersonalizedRankingCalculator.CalculateTagAffinity(["urban", "city"], profile);
        noMatchAffinity.Should().Be(0.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_CreatorAffinity_ShouldNormalizeBetweenZeroAndOne()
    {
        var creatorId = Guid.NewGuid();
        var profile = new UserInterestProfile
        {
            CreatorAffinities = new Dictionary<string, double>
            {
                [creatorId.ToString()] = 3.0
            }
        };

        var affinity = PersonalizedRankingCalculator.CalculateCreatorAffinity(creatorId, profile);
        // 3.0 / 4.0 = 0.75
        affinity.Should().BeApproximately(0.75, 0.01);

        var unknownCreatorAffinity = PersonalizedRankingCalculator.CalculateCreatorAffinity(Guid.NewGuid(), profile);
        unknownCreatorAffinity.Should().Be(0.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_NegativeAffinity_ShouldReturnOne_WhenMatched()
    {
        var blockedCreatorId = Guid.NewGuid();
        var profile = new UserInterestProfile
        {
            NegativeCreators = [blockedCreatorId],
            NegativeTags = ["spam", "spoiler"]
        };

        PersonalizedRankingCalculator.CalculateNegativeAffinity(blockedCreatorId, ["art"], profile).Should().Be(1.0);
        PersonalizedRankingCalculator.CalculateNegativeAffinity(Guid.NewGuid(), ["spam", "photo"], profile).Should().Be(1.0);
        PersonalizedRankingCalculator.CalculateNegativeAffinity(Guid.NewGuid(), ["safe", "clean"], profile).Should().Be(0.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_ExplorationBoost_ShouldBoostLowVotePhotos()
    {
        var zeroVotes = PersonalizedRankingCalculator.CalculateExplorationBoost(0, 0);
        var twoVotes = PersonalizedRankingCalculator.CalculateExplorationBoost(1, 1);
        var popular = PersonalizedRankingCalculator.CalculateExplorationBoost(10, 5);

        zeroVotes.Should().Be(1.0);
        twoVotes.Should().Be(0.6);
        popular.Should().Be(0.0);
    }

    [Fact]
    public void PersonalizedRankingCalculator_CompositeScore_ShouldFollowSpecifiedFormula()
    {
        // S = 0.25R + 0.20Q + 0.30T + 0.15C - 0.15N + 0.10E
        double r = 1.0;
        double q = 1.0;
        double t = 1.0;
        double c = 1.0;
        double n = 0.0;
        double e = 1.0;

        var maxScore = PersonalizedRankingCalculator.CalculateCompositeScore(r, q, t, c, n, e);
        maxScore.Should().BeApproximately(1.00, 0.0001);

        // With negative penalty N = 1.0
        var scoreWithPenalty = PersonalizedRankingCalculator.CalculateCompositeScore(r, q, t, c, 1.0, e);
        scoreWithPenalty.Should().BeApproximately(0.85, 0.0001);
    }

    [Fact]
    public void PersonalizedRankingCalculator_ShouldScorePreferredContentHigher()
    {
        var now = DateTime.UtcNow;
        var favoriteCreatorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var profile = new UserInterestProfile
        {
            UserId = Guid.NewGuid(),
            TagAffinities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["landscape"] = 5.0 },
            CreatorAffinities = new Dictionary<string, double> { [favoriteCreatorId.ToString()] = 5.0 },
            TotalInteractions = 10
        };

        var favoriteItem = new FeedItem
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            UserId = favoriteCreatorId,
            Caption = "Beautiful #landscape sunset",
            Upvotes = 20,
            Downvotes = 1,
            UploadedAt = now.AddHours(-1)
        };

        var randomItem = new FeedItem
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            UserId = strangerId,
            Caption = "Random photo with #architecture",
            Upvotes = 2,
            Downvotes = 0,
            UploadedAt = now.AddHours(-24)
        };

        var scoreFavorite = PersonalizedRankingCalculator.CalculateScore(favoriteItem, profile, now);
        var scoreRandom = PersonalizedRankingCalculator.CalculateScore(randomItem, profile, now);

        scoreFavorite.Should().BeGreaterThan(scoreRandom);
    }
}
