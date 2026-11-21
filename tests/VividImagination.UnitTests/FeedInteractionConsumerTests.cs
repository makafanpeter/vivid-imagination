using System.Linq.Expressions;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using VividImagination.Modules.Feed.Contracts.Events;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Modules.Feed.Features.Feeds.Consumers;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Caching;
using VividImagination.Shared.Infrastructure.Mongo;
using Xunit;

namespace VividImagination.UnitTests;

public class FeedInteractionConsumerTests
{
    private readonly Mock<IMongoRepository<UserInterestProfile, Guid>> _profileRepoMock = new();
    private readonly Mock<IMongoRepository<UserInteraction, Guid>> _interactionRepoMock = new();
    private readonly Mock<IMongoRepository<FeedItem, Guid>> _feedRepoMock = new();
    private readonly Mock<IStaticCacheManager> _cacheMock = new();
    private readonly Mock<ILogger<FeedInteractionConsumer>> _loggerMock = new();
    private readonly FeedInteractionConsumer _consumer;

    public FeedInteractionConsumerTests()
    {
        _consumer = new FeedInteractionConsumer(
            _profileRepoMock.Object,
            _interactionRepoMock.Object,
            _feedRepoMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldBeIdempotent_WhenDuplicateEventIdArrives()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingInteraction = new UserInteraction { EventId = eventId };

        _interactionRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInteraction, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInteraction);

        var contextMock = new Mock<ConsumeContext<FeedInteractionOccurredIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(new FeedInteractionOccurredIntegrationEvent(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Upvote",
            1,
            0,
            ["nature"],
            DateTime.UtcNow));
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert: No new interaction added, no profile updated
        _interactionRepoMock.Verify(r => r.AddAsync(It.IsAny<UserInteraction>(), It.IsAny<CancellationToken>()), Times.Never);
        _profileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserInterestProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _profileRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserInterestProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Upvote_ShouldIncreaseTagAndCreatorAffinities_AndInvalidateCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _interactionRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInteraction, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInteraction?)null);

        _profileRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInterestProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInterestProfile?)null);

        UserInterestProfile? savedProfile = null;
        _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserInterestProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserInterestProfile, CancellationToken>((p, _) => savedProfile = p)
            .ReturnsAsync((UserInterestProfile p, CancellationToken _) => p);

        var contextMock = new Mock<ConsumeContext<FeedInteractionOccurredIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(new FeedInteractionOccurredIntegrationEvent(
            Guid.NewGuid(),
            userId,
            photoId,
            creatorId,
            "Upvote",
            1,
            0,
            ["Nature", "Sunset"],
            DateTime.UtcNow));
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        savedProfile.Should().NotBeNull();
        savedProfile!.UserId.Should().Be(userId);
        savedProfile.TagAffinities["nature"].Should().Be(0.5);
        savedProfile.TagAffinities["sunset"].Should().Be(0.5);
        savedProfile.CreatorAffinities[creatorId.ToString()].Should().Be(1.0);
        savedProfile.TotalInteractions.Should().Be(1);

        _cacheMock.Verify(c => c.RemoveByPrefixAsync(FeedCacheDefaults.PersonalizedFeedPrefix, It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task Consume_Downvote_ShouldDecreaseAffinities_AndAddDownvotedPhotoId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _interactionRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInteraction, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInteraction?)null);

        var existingProfile = new UserInterestProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TagAffinities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["nature"] = 1.0 },
            CreatorAffinities = new Dictionary<string, double> { [creatorId.ToString()] = 1.0 },
            TotalInteractions = 2
        };

        _profileRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInterestProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var contextMock = new Mock<ConsumeContext<FeedInteractionOccurredIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(new FeedInteractionOccurredIntegrationEvent(
            Guid.NewGuid(),
            userId,
            photoId,
            creatorId,
            "Downvote",
            1,
            0,
            ["nature"],
            DateTime.UtcNow));
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        existingProfile.TagAffinities["nature"].Should().Be(0.5);
        existingProfile.CreatorAffinities[creatorId.ToString()].Should().Be(0.0);
        existingProfile.DownvotedPhotoIds.Should().Contain(photoId);
        existingProfile.TotalInteractions.Should().Be(3);

        _profileRepoMock.Verify(r => r.UpdateAsync(existingProfile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_Dwell_ShouldOnlyCountIfThreeSecondsOrMore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _interactionRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInteraction, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInteraction?)null);

        var existingProfile = new UserInterestProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalInteractions = 0
        };

        _profileRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInterestProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var shortDwellMock = new Mock<ConsumeContext<FeedInteractionOccurredIntegrationEvent>>();
        shortDwellMock.Setup(c => c.Message).Returns(new FeedInteractionOccurredIntegrationEvent(
            Guid.NewGuid(),
            userId,
            photoId,
            creatorId,
            "Dwell",
            1,
            1500, // < 3000ms
            ["nature"],
            DateTime.UtcNow));
        shortDwellMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(shortDwellMock.Object);

        // Assert: No tag affinity added
        existingProfile.TagAffinities.Should().NotContainKey("nature");

        var longDwellMock = new Mock<ConsumeContext<FeedInteractionOccurredIntegrationEvent>>();
        longDwellMock.Setup(c => c.Message).Returns(new FeedInteractionOccurredIntegrationEvent(
            Guid.NewGuid(),
            userId,
            photoId,
            creatorId,
            "Dwell",
            1,
            4000, // >= 3000ms
            ["nature"],
            DateTime.UtcNow));
        longDwellMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(longDwellMock.Object);

        // Assert: Tag affinity added
        existingProfile.TagAffinities["nature"].Should().Be(0.2);
    }
}
