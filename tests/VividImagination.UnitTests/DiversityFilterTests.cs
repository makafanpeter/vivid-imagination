using FluentAssertions;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Shared.Contract.Identity.Enums;
using Xunit;

namespace VividImagination.UnitTests;

public class DiversityFilterTests
{
    [Fact]
    public void DiversityFilter_ShouldExclude_DownvotedAndHiddenPhotos()
    {
        var downvotedPhotoId = Guid.NewGuid();
        var hiddenPhotoId = Guid.NewGuid();
        var normalPhotoId = Guid.NewGuid();

        var profile = new UserInterestProfile
        {
            DownvotedPhotoIds = [downvotedPhotoId],
            HiddenPhotoIds = [hiddenPhotoId]
        };

        var candidates = new List<FeedItem>
        {
            new() { Id = downvotedPhotoId, PhotoId = downvotedPhotoId, Caption = "Downvoted", Verdict = "Approved" },
            new() { Id = hiddenPhotoId, PhotoId = hiddenPhotoId, Caption = "Hidden", Verdict = "Approved" },
            new() { Id = normalPhotoId, PhotoId = normalPhotoId, Caption = "Normal", Verdict = "Approved" }
        };

        var result = DiversityFilter.FilterAndDiversify(candidates, profile, null, null, DateTime.UtcNow);

        result.Should().HaveCount(1);
        result[0].PhotoId.Should().Be(normalPhotoId);
    }

    [Fact]
    public void DiversityFilter_ShouldExclude_RejectedPhotos()
    {
        var rejectedId = Guid.NewGuid();
        var approvedId = Guid.NewGuid();

        var candidates = new List<FeedItem>
        {
            new() { Id = rejectedId, PhotoId = rejectedId, Caption = "Rejected photo", Verdict = "Rejected" },
            new() { Id = approvedId, PhotoId = approvedId, Caption = "Approved photo", Verdict = "Approved" }
        };

        var result = DiversityFilter.FilterAndDiversify(candidates, null, null, null, DateTime.UtcNow);

        result.Should().HaveCount(1);
        result[0].PhotoId.Should().Be(approvedId);
    }

    [Fact]
    public void DiversityFilter_ShouldRespect_SafeBrowsingHideSensitive()
    {
        var safeId = Guid.NewGuid();
        var unsafeId = Guid.NewGuid();

        var candidates = new List<FeedItem>
        {
            new() { Id = safeId, PhotoId = safeId, Caption = "Safe", Verdict = "Approved", IsUnsafe = false, BlurRequired = false },
            new() { Id = unsafeId, PhotoId = unsafeId, Caption = "Suggestive", Verdict = "Approved", IsUnsafe = true, BlurRequired = true }
        };

        var result = DiversityFilter.FilterAndDiversify(candidates, null, SafeBrowsingMode.HideSensitive, null, DateTime.UtcNow);

        result.Should().HaveCount(1);
        result[0].PhotoId.Should().Be(safeId);
    }

    [Fact]
    public void DiversityFilter_ShouldLimitConsecutivePhotosFromSameCreator()
    {
        var creatorA = Guid.NewGuid();
        var creatorB = Guid.NewGuid();
        var creatorC = Guid.NewGuid();

        // 4 photos from Creator A with high score, 1 from Creator B, 1 from Creator C
        var candidates = new List<FeedItem>
        {
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorA, Caption = "#nature 1", Upvotes = 100, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorA, Caption = "#nature 2", Upvotes = 95, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorA, Caption = "#nature 3", Upvotes = 90, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorA, Caption = "#nature 4", Upvotes = 85, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorB, Caption = "#city 1", Upvotes = 50, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = Guid.NewGuid(), PhotoId = Guid.NewGuid(), UserId = creatorC, Caption = "#art 1", Upvotes = 40, UploadedAt = DateTime.UtcNow, Verdict = "Approved" }
        };

        var result = DiversityFilter.FilterAndDiversify(candidates, null, null, null, DateTime.UtcNow);

        // Verify that no 3 consecutive items share the same creator
        for (int i = 0; i < result.Count - 2; i++)
        {
            var consecutiveThreeSameCreator = result[i].UserId == result[i + 1].UserId &&
                                              result[i + 1].UserId == result[i + 2].UserId;
            consecutiveThreeSameCreator.Should().BeFalse(
                $"Index {i}, {i+1}, {i+2} should not all belong to creator {result[i].UserId}");
        }
    }
}
