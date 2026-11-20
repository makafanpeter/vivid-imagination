using System.Linq.Expressions;
using FluentAssertions;
using MediatR;
using Moq;
using MongoDB.Driver;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Features.Feeds.Queries;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Feed.Ranking;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Caching;
using VividImagination.Shared.Infrastructure.Mongo;
using Xunit;

namespace VividImagination.UnitTests;

public class PersonalizedFeedQueryTests
{
    private readonly Mock<IMongoRepository<FeedItem, Guid>> _feedRepoMock = new();
    private readonly Mock<IMongoRepository<UserInterestProfile, Guid>> _profileRepoMock = new();
    private readonly Mock<ICandidateGenerator> _candidateGeneratorMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IStaticCacheManager> _cacheMock = new();

    private readonly GetPersonalizedFeedQueryHandler _handler;

    public PersonalizedFeedQueryTests()
    {
        _handler = new GetPersonalizedFeedQueryHandler(
            _feedRepoMock.Object,
            _profileRepoMock.Object,
            _candidateGeneratorMock.Object,
            _mediatorMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToGlobalFeed_WhenUserHasFewerThanThreeInteractions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var coldProfile = new UserInterestProfile
        {
            UserId = userId,
            TotalInteractions = 2 // < 3 interactions = cold start
        };

        _profileRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInterestProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coldProfile);

        var fallbackDto = new List<FeedItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "key1", "hash1", "Fallback photo", 10, 0, 5.0, false, false, null, null, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetFeedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackDto);

        var query = new GetPersonalizedFeedQuery(Page: 1, PageSize: 20, UserId: userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(fallbackDto);
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetFeedQuery>(q => q.Page == 1 && q.PageSize == 20),
            It.IsAny<CancellationToken>()), Times.Once);
        _candidateGeneratorMock.Verify(c => c.GenerateCandidatesAsync(It.IsAny<UserInterestProfile>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToGlobalFeed_WhenPersonalisationIsDisabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fallbackDto = new List<FeedItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "key2", "hash2", "Global feed photo", 5, 0, 2.0, false, false, null, null, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetFeedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackDto);

        var query = new GetPersonalizedFeedQuery(Page: 1, PageSize: 20, UserId: userId, IsPersonalisationEnabled: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(fallbackDto);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetFeedQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _profileRepoMock.Verify(p => p.FindOneAsync(It.IsAny<Expression<Func<UserInterestProfile, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateAndRankCandidates_ForActiveUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activeProfile = new UserInterestProfile
        {
            UserId = userId,
            TotalInteractions = 10,
            TagAffinities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["nature"] = 5.0 }
        };

        _profileRepoMock.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<UserInterestProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeProfile);

        var photoId1 = Guid.NewGuid();
        var photoId2 = Guid.NewGuid();

        var candidates = new List<FeedItem>
        {
            new() { Id = photoId1, PhotoId = photoId1, UserId = Guid.NewGuid(), Caption = "Nice #nature view", Upvotes = 10, UploadedAt = DateTime.UtcNow, Verdict = "Approved" },
            new() { Id = photoId2, PhotoId = photoId2, UserId = Guid.NewGuid(), Caption = "Urban #city", Upvotes = 2, UploadedAt = DateTime.UtcNow.AddDays(-2), Verdict = "Approved" }
        };

        _candidateGeneratorMock.Setup(c => c.GenerateCandidatesAsync(activeProfile, 300, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var query = new GetPersonalizedFeedQuery(Page: 1, PageSize: 20, UserId: userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].PhotoId.Should().Be(photoId1); // Tag-matched photo should rank first
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetFeedQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
