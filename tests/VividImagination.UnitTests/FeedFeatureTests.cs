using System.Linq.Expressions;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using VividImagination.Modules.Feed.Features.Feeds.Caching;
using VividImagination.Modules.Feed.Features.Feeds.Commands;
using VividImagination.Modules.Feed.Features.Feeds.Consumers;
using VividImagination.Modules.Feed.Features.Feeds.Dtos;
using VividImagination.Modules.Feed.Features.Feeds.Queries;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Moderation.Contracts.Events;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Shared.Domain.Infrastructure;
using VividImagination.Shared.Infrastructure.Mongo;
using Xunit;

namespace VividImagination.UnitTests;

public class FeedFeatureTests
{
    [Fact]
    public void GetFeedQuery_CacheRequest_Properties_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        var query = new GetFeedQuery(2, 50);

        // Assert
        query.CacheKey.Should().Be(FeedCacheDefaults.FeedPageCacheKey);
        query.CacheKeyParameters.Should().ContainInOrder([2, 50]);
        FeedCacheDefaults.FeedPrefix.Should().Be("vivid:feed");
    }

    [Fact]
    public void InvalidateFeedCacheCommand_ShouldConfigurePrefixCorrectly()
    {
        // Arrange & Act
        var command = new InvalidateFeedCacheCommand();

        // Assert
        command.Prefix.Should().Be(FeedCacheDefaults.FeedPrefix);
        command.Prefixes.Should().Contain(FeedCacheDefaults.FeedPrefix);
    }

    [Fact]
    public void GetFeedQueryValidator_ShouldValidatePaginationParameters()
    {
        // Arrange
        var validator = new GetFeedQueryValidator();

        // Act & Assert
        validator.Validate(new GetFeedQuery(0, 20)).IsValid.Should().BeFalse();
        validator.Validate(new GetFeedQuery(1, 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetFeedQuery(1, 101)).IsValid.Should().BeFalse();
        validator.Validate(new GetFeedQuery(1, 20)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeedQueryHandler_ShouldReturnMappedDtos()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var photoId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var feedItems = new List<FeedItem>
        {
            new()
            {
                Id = photoId,
                PhotoId = photoId,
                UserId = userId,
                StorageKey = "photos/test.jpg",
                BlurHash = "L6PZfSi_.AyE_3t7t7R**0o#DgR4",
                Caption = "Sunset at the beach",
                Upvotes = 10,
                Downvotes = 1,
                Score = 9.5,
                IsUnsafe = false,
                BlurRequired = true,
                SafetyCategory = "Suggestive",
                Verdict = "Approved",
                UploadedAt = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        var pagedList = new PagedList<FeedItem>(feedItems, 0, 20, 1);

        repoMock.Setup(r => r.FindPagedAsync(
                It.IsAny<Expression<Func<FeedItem, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<SortDefinition<FeedItem>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var handler = new GetFeedQueryHandler(repoMock.Object);

        // Act
        var result = await handler.Handle(new GetFeedQuery(1, 20), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(photoId);
        dto.PhotoId.Should().Be(photoId);
        dto.UserId.Should().Be(userId);
        dto.Caption.Should().Be("Sunset at the beach");
        dto.Upvotes.Should().Be(10);
        dto.Score.Should().Be(9.5);
        dto.BlurRequired.Should().BeTrue();
        dto.SensitiveContentWarning.Should().Be("Sensitive Content: Suggestive");
    }

    [Fact]
    public async Task GetFeedQueryHandler_WithHideSensitive_ShouldFilterOutSensitiveItems()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var safePhotoId = Guid.NewGuid();
        var sensitivePhotoId = Guid.NewGuid();

        var feedItems = new List<FeedItem>
        {
            new()
            {
                Id = safePhotoId,
                PhotoId = safePhotoId,
                UserId = Guid.NewGuid(),
                Caption = "Safe Landscape",
                IsUnsafe = false,
                BlurRequired = false,
                Verdict = "Approved",
                UploadedAt = DateTime.UtcNow
            },
            new()
            {
                Id = sensitivePhotoId,
                PhotoId = sensitivePhotoId,
                UserId = Guid.NewGuid(),
                Caption = "Suggestive Beach",
                IsUnsafe = true,
                BlurRequired = true,
                SafetyCategory = "Suggestive",
                Verdict = "TaggedUnsafe",
                UploadedAt = DateTime.UtcNow
            }
        };

        var pagedList = new PagedList<FeedItem>(feedItems, 0, 20, 2);

        repoMock.Setup(r => r.FindPagedAsync(
                It.IsAny<Expression<Func<FeedItem, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<SortDefinition<FeedItem>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var handler = new GetFeedQueryHandler(repoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetFeedQuery(1, 20, VividImagination.Shared.Contract.Identity.Enums.SafeBrowsingMode.HideSensitive),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(safePhotoId);
    }

    [Fact]
    public async Task GetFeedQueryHandler_WithAlwaysShowSensitive_ShouldUnblurSensitiveItems()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var sensitivePhotoId = Guid.NewGuid();

        var feedItems = new List<FeedItem>
        {
            new()
            {
                Id = sensitivePhotoId,
                PhotoId = sensitivePhotoId,
                UserId = Guid.NewGuid(),
                Caption = "Suggestive Beach",
                IsUnsafe = true,
                BlurRequired = true,
                SafetyCategory = "Suggestive",
                Verdict = "TaggedUnsafe",
                UploadedAt = DateTime.UtcNow
            }
        };

        var pagedList = new PagedList<FeedItem>(feedItems, 0, 20, 1);

        repoMock.Setup(r => r.FindPagedAsync(
                It.IsAny<Expression<Func<FeedItem, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<SortDefinition<FeedItem>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var handler = new GetFeedQueryHandler(repoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetFeedQuery(1, 20, VividImagination.Shared.Contract.Identity.Enums.SafeBrowsingMode.AlwaysShowSensitive),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].BlurRequired.Should().BeFalse();
        result[0].SensitiveContentWarning.Should().BeNull();
    }

    [Fact]
    public async Task PhotoUploadedFeedConsumer_ShouldSetIdAndPublishInvalidation()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<PhotoUploadedFeedConsumer>>();
        var consumer = new PhotoUploadedFeedConsumer(repoMock.Object, mediatorMock.Object, loggerMock.Object);

        var photoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var integrationEvent = new PhotoUploadedIntegrationEvent(
            photoId,
            userId,
            "photos/photo.png",
            "L6PZfSi_",
            "My vacation",
            DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<PhotoUploadedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(integrationEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        FeedItem? capturedEntity = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((item, _) => capturedEntity = item)
            .ReturnsAsync((FeedItem item, CancellationToken _) => item);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.Id.Should().Be(photoId);
        capturedEntity.PhotoId.Should().Be(photoId);
        capturedEntity.UserId.Should().Be(userId);
        capturedEntity.Caption.Should().Be("My vacation");
        capturedEntity.Verdict.Should().Be("Pending");

        mediatorMock.Verify(m => m.Send(It.IsAny<InvalidateFeedCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhotoModeratedFeedConsumer_Rejected_ShouldDeleteAndInvalidateCache()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<PhotoModeratedFeedConsumer>>();
        var consumer = new PhotoModeratedFeedConsumer(repoMock.Object, mediatorMock.Object, loggerMock.Object);

        var photoId = Guid.NewGuid();
        var integrationEvent = new PhotoModeratedIntegrationEvent(
            photoId,
            true,
            true,
            "Graphic violence",
            "Rejected",
            0.98,
            DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<PhotoModeratedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(integrationEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        repoMock.Verify(r => r.DeleteByIdAsync(photoId, It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(It.IsAny<InvalidateFeedCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhotoVotedFeedConsumer_ShouldUpdateVotesAndInvalidateCache()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<PhotoVotedFeedConsumer>>();
        var consumer = new PhotoVotedFeedConsumer(repoMock.Object, mediatorMock.Object, loggerMock.Object);

        var photoId = Guid.NewGuid();
        var existingItem = new FeedItem
        {
            Id = photoId,
            PhotoId = photoId,
            Upvotes = 2,
            Downvotes = 0,
            Score = 2.0,
            UploadedAt = DateTime.UtcNow.AddHours(-1)
        };

        repoMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItem);

        var integrationEvent = new PhotoVotedIntegrationEvent(
            photoId,
            12345L,
            1,
            5,
            1,
            DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<PhotoVotedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(integrationEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        existingItem.Upvotes.Should().Be(5);
        existingItem.Downvotes.Should().Be(1);
        repoMock.Verify(r => r.UpdateAsync(existingItem, It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(It.IsAny<InvalidateFeedCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhotoDeletedFeedConsumer_ShouldDeleteAndInvalidateCache()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var mediatorMock = new Mock<IMediator>();
        var consumer = new PhotoDeletedFeedConsumer(repoMock.Object, mediatorMock.Object);

        var photoId = Guid.NewGuid();
        var integrationEvent = new PhotoDeletedIntegrationEvent(
            photoId,
            "photos/photo-to-delete.png",
            DateTime.UtcNow);

        var contextMock = new Mock<ConsumeContext<PhotoDeletedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(integrationEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        repoMock.Verify(r => r.DeleteByIdAsync(photoId, It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(It.IsAny<InvalidateFeedCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPersonalizedFeedQueryHandler_ShouldFilterExcludedPhotos()
    {
        // Arrange
        var repoMock = new Mock<IMongoRepository<FeedItem, Guid>>();
        var photoId1 = Guid.NewGuid();
        var photoId2 = Guid.NewGuid();

        var feedItems = new List<FeedItem>
        {
            new() { Id = photoId1, PhotoId = photoId1, Caption = "Photo 1", Verdict = "Approved" },
            new() { Id = photoId2, PhotoId = photoId2, Caption = "Photo 2", Verdict = "Approved" }
        };

        var pagedList = new PagedList<FeedItem>(feedItems, 0, 20, 2);
        repoMock.Setup(r => r.FindPagedAsync(
                It.IsAny<Expression<Func<FeedItem, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<SortDefinition<FeedItem>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var handler = new GetPersonalizedFeedQueryHandler(repoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetPersonalizedFeedQuery(1, 20, ExcludedPhotoIds: [photoId1]),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].PhotoId.Should().Be(photoId2);
    }

    [Fact]
    public void GetPersonalizedFeedQueryValidator_ShouldValidatePaginationParameters()
    {
        var validator = new GetPersonalizedFeedQueryValidator();

        validator.Validate(new GetPersonalizedFeedQuery(0, 20)).IsValid.Should().BeFalse();
        validator.Validate(new GetPersonalizedFeedQuery(1, 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetPersonalizedFeedQuery(1, 101)).IsValid.Should().BeFalse();
        validator.Validate(new GetPersonalizedFeedQuery(1, 20)).IsValid.Should().BeTrue();
    }
}
