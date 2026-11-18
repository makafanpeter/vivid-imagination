using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Modules.Social.Features.Reports.Commands;
using VividImagination.Modules.Social.Features.Reports.Queries;
using VividImagination.Modules.Social.Features.Reports.Validators;
using VividImagination.Modules.Social.Features.Votes.Commands;
using VividImagination.Modules.Social.Features.Votes.Queries;
using VividImagination.Modules.Social.Features.Votes.Validators;
using VividImagination.Modules.Social.Persistence;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using Xunit;

namespace VividImagination.UnitTests;

public class SocialFeatureTests
{
    private static SocialDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SocialDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SocialDbContext(options);
    }

    private static Mock<ICurrentUser> CreateCurrentUserMock(long userId)
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(c => c.CurrentUser).Returns(new Profile { Sub = userId.ToString() });
        mock.Setup(c => c.GetUserId()).Returns(userId);
        return mock;
    }

    [Fact]
    public async Task VoteOnPhoto_Should_Persist_Upvote_And_Publish_IntegrationEvent()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        long userId = 1001;
        var currentUserMock = CreateCurrentUserMock(userId);

        var handler = new VoteOnPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);
        var photoId = Guid.NewGuid();

        var result = await handler.Handle(new VoteOnPhotoCommand(photoId, 1), CancellationToken.None);

        result.PhotoId.Should().Be(photoId);
        result.Upvotes.Should().Be(1);
        result.Downvotes.Should().Be(0);

        var savedVote = await db.Votes.FirstOrDefaultAsync(v => v.PhotoId == photoId && v.UserId == userId);
        savedVote.Should().NotBeNull();
        savedVote!.Value.Should().Be(1);

        publisherMock.Verify(p => p.Publish(
            It.Is<PhotoVotedIntegrationEvent>(e =>
                e.PhotoId == photoId &&
                e.UserId == userId &&
                e.VoteDelta == 1 &&
                e.TotalUpvotes == 1 &&
                e.TotalDownvotes == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoteOnPhoto_Should_Update_Existing_Vote_From_Upvote_To_Downvote()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        long userId = 2002;
        var currentUserMock = CreateCurrentUserMock(userId);

        var handler = new VoteOnPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);
        var photoId = Guid.NewGuid();

        // Initial Upvote
        await handler.Handle(new VoteOnPhotoCommand(photoId, 1), CancellationToken.None);

        // Switch to Downvote
        var result = await handler.Handle(new VoteOnPhotoCommand(photoId, -1), CancellationToken.None);

        result.PhotoId.Should().Be(photoId);
        result.Upvotes.Should().Be(0);
        result.Downvotes.Should().Be(1);

        publisherMock.Verify(p => p.Publish(
            It.Is<PhotoVotedIntegrationEvent>(e =>
                e.PhotoId == photoId &&
                e.UserId == userId &&
                e.VoteDelta == -2 && // Changed from +1 to -1 => delta is -2
                e.TotalUpvotes == 0 &&
                e.TotalDownvotes == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoteOnPhoto_Should_Throw_Unauthorized_When_User_Not_Logged_In()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(c => c.CurrentUser).Returns((Profile?)null);

        var handler = new VoteOnPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);

        var act = () => handler.Handle(new VoteOnPhotoCommand(Guid.NewGuid(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task ReportPhoto_Should_Persist_Report_And_Publish_IntegrationEvent()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        long userId = 3003;
        var currentUserMock = CreateCurrentUserMock(userId);

        var handler = new ReportPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);
        var photoId = Guid.NewGuid();

        var result = await handler.Handle(new ReportPhotoCommand(photoId, "Inappropriate image"), CancellationToken.None);

        result.PhotoId.Should().Be(photoId);
        result.TotalReports.Should().Be(1);

        var savedReport = await db.Reports.FirstOrDefaultAsync(r => r.PhotoId == photoId && r.ReporterUserId == userId);
        savedReport.Should().NotBeNull();
        savedReport!.Reason.Should().Be("Inappropriate image");

        publisherMock.Verify(p => p.Publish(
            It.Is<PhotoReportedIntegrationEvent>(e =>
                e.PhotoId == photoId &&
                e.ReporterUserId == userId &&
                e.Reason == "Inappropriate image" &&
                e.TotalReports == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReportPhoto_Should_Be_Idempotent_For_Same_User_And_Photo()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        long userId = 4004;
        var currentUserMock = CreateCurrentUserMock(userId);

        var handler = new ReportPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);
        var photoId = Guid.NewGuid();

        // First report
        await handler.Handle(new ReportPhotoCommand(photoId, "Spam"), CancellationToken.None);

        // Second report from same user
        var result = await handler.Handle(new ReportPhotoCommand(photoId, "Spam duplicate"), CancellationToken.None);

        result.TotalReports.Should().Be(1);
        var count = await db.Reports.CountAsync(r => r.PhotoId == photoId && r.ReporterUserId == userId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Queries_Should_Return_Aggregates()
    {
        using var db = CreateInMemoryDbContext();
        var photoId = Guid.NewGuid();

        db.Votes.AddRange(
            new Modules.Social.Persistence.Entities.PhotoVote { Id = Guid.NewGuid(), PhotoId = photoId, UserId = 1, Value = 1, VotedAt = DateTime.UtcNow },
            new Modules.Social.Persistence.Entities.PhotoVote { Id = Guid.NewGuid(), PhotoId = photoId, UserId = 2, Value = 1, VotedAt = DateTime.UtcNow },
            new Modules.Social.Persistence.Entities.PhotoVote { Id = Guid.NewGuid(), PhotoId = photoId, UserId = 3, Value = -1, VotedAt = DateTime.UtcNow }
        );

        db.Reports.AddRange(
            new Modules.Social.Persistence.Entities.PhotoReport { Id = Guid.NewGuid(), PhotoId = photoId, ReporterUserId = 10, Reason = "Flag 1", ReportedAt = DateTime.UtcNow },
            new Modules.Social.Persistence.Entities.PhotoReport { Id = Guid.NewGuid(), PhotoId = photoId, ReporterUserId = 11, Reason = "Flag 2", ReportedAt = DateTime.UtcNow }
        );

        await db.SaveChangesAsync();

        var voteQueryHandler = new GetVoteOnPhotoQueryHandler(db);
        var voteResult = await voteQueryHandler.Handle(new GetVoteOnPhotoQuery(photoId), CancellationToken.None);

        voteResult.PhotoId.Should().Be(photoId);
        voteResult.Upvotes.Should().Be(2);
        voteResult.Downvotes.Should().Be(1);

        var reportQueryHandler = new GetPhotoReportQueryHandler(db);
        var reportResult = await reportQueryHandler.Handle(new GetPhotoReportQuery(photoId), CancellationToken.None);

        reportResult.PhotoId.Should().Be(photoId);
        reportResult.TotalReports.Should().Be(2);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(2, false)]
    [InlineData(-2, false)]
    public void VoteOnPhotoCommandValidator_Should_Validate_Allowed_Values(int value, bool shouldBeValid)
    {
        var validator = new VoteOnPhotoCommandValidator();
        var command = new VoteOnPhotoCommand(Guid.NewGuid(), value);

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().Be(shouldBeValid);
    }

    [Fact]
    public async Task VoteOnPhoto_Should_Cancel_Vote_When_Value_Is_Zero()
    {
        using var db = CreateInMemoryDbContext();
        var publisherMock = new Mock<IPublishEndpoint>();
        long userId = 1001;
        var currentUserMock = CreateCurrentUserMock(userId);

        var handler = new VoteOnPhotoCommandHandler(db, publisherMock.Object, currentUserMock.Object);
        var photoId = Guid.NewGuid();

        // Upvote first
        await handler.Handle(new VoteOnPhotoCommand(photoId, 1), CancellationToken.None);
        var upvote = await db.Votes.FirstOrDefaultAsync(v => v.PhotoId == photoId && v.UserId == userId);
        upvote.Should().NotBeNull();

        // Cancel vote
        var result = await handler.Handle(new VoteOnPhotoCommand(photoId, 0), CancellationToken.None);

        result.Upvotes.Should().Be(0);
        result.Downvotes.Should().Be(0);

        var canceledVote = await db.Votes.FirstOrDefaultAsync(v => v.PhotoId == photoId && v.UserId == userId);
        canceledVote.Should().BeNull();

        publisherMock.Verify(p => p.Publish(It.Is<PhotoVotedIntegrationEvent>(
            e => e.PhotoId == photoId && e.VoteDelta == -1 && e.TotalUpvotes == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ReportPhotoCommandValidator_Should_Validate_Reason_Length()
    {
        var validator = new ReportPhotoCommandValidator();
        var validCommand = new ReportPhotoCommand(Guid.NewGuid(), "Valid reason");
        var emptyPhotoCommand = new ReportPhotoCommand(Guid.Empty, "Valid reason");
        var longReasonCommand = new ReportPhotoCommand(Guid.NewGuid(), new string('x', 501));

        validator.Validate(validCommand).IsValid.Should().BeTrue();
        validator.Validate(emptyPhotoCommand).IsValid.Should().BeFalse();
        validator.Validate(longReasonCommand).IsValid.Should().BeFalse();
    }
}
