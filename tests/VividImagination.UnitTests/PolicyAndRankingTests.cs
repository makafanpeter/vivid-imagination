using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Moderation.Features.Reviews.Commands;
using VividImagination.Modules.Moderation.Features.Reviews.Consumers;
using VividImagination.Modules.Moderation.Persistence;
using VividImagination.Modules.Moderation.Persistence.Documents;
using VividImagination.Modules.Moderation.Services;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Shared.Infrastructure.Mongo;
using Xunit;

namespace VividImagination.UnitTests;

public class PolicyAndRankingTests
{
    [Theory]
    [InlineData("normal_landscape.jpg", "Approved", false, false)]
    [InlineData("beach_swimwear_suggestive.jpg", "TaggedUnsafe", true, true)]
    [InlineData("explicit_nsfw_content.jpg", "Rejected", true, true)]
    public async Task AmazonRekognitionService_Should_Apply_Three_Tier_Policy(
        string storageKey,
        string expectedVerdict,
        bool expectedIsUnsafe,
        bool expectedBlurRequired)
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new AmazonRekognitionService(configuration, NullLogger<AmazonRekognitionService>.Instance);

        var result = await service.InspectPhotoAsync("vivid-imagination-photos", storageKey);

        result.Verdict.Should().Be(expectedVerdict);
        result.IsUnsafe.Should().Be(expectedIsUnsafe);
        result.BlurRequired.Should().Be(expectedBlurRequired);
    }

    [Fact]
    public void FeedRanking_Formula_Should_Calculate_Score_Correctly()
    {
        // Score = (Upvotes - Downvotes) / (AgeInHours + 2)^1.5
        var uploadedAt = DateTime.UtcNow; // Age ~ 0 hours
        int upvotes = 100;
        int downvotes = 20;

        var score = FeedRankingCalculator.CalculateScore(upvotes, downvotes, uploadedAt);

        // Expected score: 80 / (2)^1.5 = 80 / 2.828427 ~= 28.284
        score.Should().BeApproximately(28.284, 0.1);
    }

    [Fact]
    public void FeedRanking_Older_Posts_Should_Decay_Even_With_Equal_Votes()
    {
        int upvotes = 50;
        int downvotes = 0;

        var freshScore = FeedRankingCalculator.CalculateScore(upvotes, downvotes, DateTime.UtcNow);
        var oldScore = FeedRankingCalculator.CalculateScore(upvotes, downvotes, DateTime.UtcNow.AddHours(-24));

        freshScore.Should().BeGreaterThan(oldScore);
    }

    [Fact]
    public async Task Two_Flag_Rule_Should_Trigger_TaggedUnsafe_At_Two_Reports()
    {
        var repositoryMock = new Mock<IMongoRepository<ModerationReview, Guid>>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var photoId = Guid.NewGuid();

        var existingReview = new ModerationReview
        {
            Id = photoId,
            PhotoId = photoId,
            Verdict = "Approved",
            IsUnsafe = false,
            BlurRequired = false,
            ReportCount = 1
        };

        repositoryMock
            .Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        var consumer = new PhotoReportedConsumer(
            repositoryMock.Object,
            publishEndpointMock.Object,
            NullLogger<PhotoReportedConsumer>.Instance);

        var consumeContextMock = new Mock<ConsumeContext<PhotoReportedIntegrationEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(new PhotoReportedIntegrationEvent(
            photoId,
            1001L,
            "Sensitive content",
            2, // 2nd report!
            DateTime.UtcNow));

        await consumer.Consume(consumeContextMock.Object);

        existingReview.Verdict.Should().Be("TaggedUnsafe");
        existingReview.IsUnsafe.Should().BeTrue();
        existingReview.BlurRequired.Should().BeTrue();
        existingReview.SafetyCategory.Should().Contain("2-Flag Rule");

        // Verify PhotoModeratedIntegrationEvent was published
        publishEndpointMock.Verify(p => p.Publish(
            It.Is<PhotoModeratedIntegrationEvent>(e => e.PhotoId == photoId && e.BlurRequired == true && e.IsUnsafe == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateModerationVerdict_Should_Update_Review_And_Publish_PhotoModeratedIntegrationEvent()
    {
        var repositoryMock = new Mock<IMongoRepository<ModerationReview, Guid>>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var photoId = Guid.NewGuid();

        var existingReview = new ModerationReview
        {
            Id = photoId,
            PhotoId = photoId,
            Verdict = "TaggedUnsafe",
            IsUnsafe = true,
            BlurRequired = true,
            SafetyCategory = "Suggestive"
        };

        repositoryMock
            .Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        var handler = new UpdateModerationVerdictCommandHandler(
            repositoryMock.Object,
            publishEndpointMock.Object,
            NullLogger<UpdateModerationVerdictCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateModerationVerdictCommand(photoId, "Approved", "False positive approved by moderator", false),
            CancellationToken.None);

        result.Verdict.Should().Be("Approved");
        result.IsUnsafe.Should().BeFalse();
        result.BlurRequired.Should().BeFalse();

        repositoryMock.Verify(r => r.UpdateAsync(existingReview, It.IsAny<CancellationToken>()), Times.Once);
        publishEndpointMock.Verify(p => p.Publish(
            It.Is<PhotoModeratedIntegrationEvent>(e => e.PhotoId == photoId && e.Verdict == "Approved" && !e.IsUnsafe),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
