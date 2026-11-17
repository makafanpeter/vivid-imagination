using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using MongoDB.Driver;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Media.Features.Photos.Commands;
using VividImagination.Modules.Media.Features.Photos.Validations;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Helpers;
using Xunit;
using UserProfile = VividImagination.Shared.Contract.Identity.Profile;

namespace VividImagination.UnitTests;

public class MediaFeatureTests
{
    [Fact]
    public async Task UploadDocument_Should_Persist_FileEntry_And_Publish_PhotoUploadedIntegrationEvent()
    {
        var fileServiceManagerMock = new Mock<IFileServiceManager>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var fileRepositoryMock = new Mock<IMongoRepository<FileEntry, Guid>>();
        var mapperMock = new Mock<IMapper>();
        var currentUserMock = new Mock<ICurrentUser>();
        var linkGeneratorMock = new Mock<LinkGenerator>();
        var webHelperMock = new Mock<IWebHelper>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        long userId = 5005;
        currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        currentUserMock.Setup(c => c.CurrentUser).Returns(new UserProfile { Sub = userId.ToString() });
        webHelperMock.Setup(w => w.GetCurrentIpAddress()).Returns("127.0.0.1");
        webHelperMock.Setup(w => w.IsCurrentConnectionSecured()).Returns(false);

        mapperMock.Setup(m => m.Map<UploadDocumentViewModel>(It.IsAny<FileEntry>()))
            .Returns((FileEntry entry) => new UploadDocumentViewModel
            {
                Id = entry.Id,
                FileName = entry.FileName,
                Description = entry.Description,
                ContentType = entry.ContentType,
                FileExtension = entry.FileExtension
            });

        var handler = new UploadDocumentCommandHandler(
            fileServiceManagerMock.Object,
            httpContextAccessorMock.Object,
            fileRepositoryMock.Object,
            mapperMock.Object,
            currentUserMock.Object,
            linkGeneratorMock.Object,
            webHelperMock.Object,
            publishEndpointMock.Object);

        using var memoryStream = new MemoryStream([1, 2, 3, 4]);
        var command = new UploadDocumentCommand
        {
            FileName = "vacation.jpg",
            Name = "Vacation photo",
            Description = "Sunny beach",
            ContentType = "image/jpeg",
            ContentLength = 4,
            ContentStream = memoryStream,
            FileExtension = ".jpg",
            ContainerName = "photos"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Be("vacation.jpg");
        result.Description.Should().Be("Sunny beach");

        // Verify repository persistence
        fileRepositoryMock.Verify(r => r.AddAsync(
            It.Is<FileEntry>(f => f.FileName == "vacation.jpg" && f.OwnerId == userId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify PhotoUploadedIntegrationEvent was published
        publishEndpointMock.Verify(p => p.Publish(
            It.Is<PhotoUploadedIntegrationEvent>(e =>
                e.PhotoId == result.Id &&
                e.Caption == "Sunny beach" &&
                !string.IsNullOrEmpty(e.BlurHash)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocument_Should_Update_Document_To_Deleted_And_Publish_PhotoDeletedIntegrationEvent()
    {
        var fileServiceManagerMock = new Mock<IFileServiceManager>();
        var fileRepositoryMock = new Mock<IMongoRepository<FileEntry, Guid>>();
        var mapperMock = new Mock<IMapper>();
        var currentUserMock = new Mock<ICurrentUser>();
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        var photoId = Guid.NewGuid();
        var existingFile = new FileEntry
        {
            Id = photoId,
            Name = "photo_to_delete",
            FileName = "photo_to_delete.jpg",
            FileLocation = "photos/photo_to_delete.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            OwnerId = "123",
            Deleted = false
        };

        fileRepositoryMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFile);

        currentUserMock.Setup(c => c.User).Returns(new ClaimsPrincipal());
        currentUserMock.Setup(c => c.CurrentUser).Returns(new UserProfile { PreferredUsername = "testuser" });

        authorizationServiceMock.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var handler = new DeleteDocumentCommandHandler(
            fileServiceManagerMock.Object,
            fileRepositoryMock.Object,
            currentUserMock.Object,
            authorizationServiceMock.Object,
            publishEndpointMock.Object);

        await handler.Handle(new DeleteDocumentCommand { Id = photoId }, CancellationToken.None);

        existingFile.Deleted.Should().BeTrue();

        // Verify fileServiceManager called delete
        fileServiceManagerMock.Verify(f => f.DeleteAsync(existingFile, It.IsAny<CancellationToken>()), Times.Once);

        // Verify fileRepository.UpdateAsync called (NOT AddAsync)
        fileRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<FileEntry>(f => f.Id == photoId && f.Deleted == true),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify PhotoDeletedIntegrationEvent was published
        publishEndpointMock.Verify(p => p.Publish(
            It.Is<PhotoDeletedIntegrationEvent>(e => e.PhotoId == photoId && e.StorageKey == "photos/photo_to_delete.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocument_Should_Throw_NotFoundException_When_File_Missing()
    {
        var fileServiceManagerMock = new Mock<IFileServiceManager>();
        var fileRepositoryMock = new Mock<IMongoRepository<FileEntry, Guid>>();
        var mapperMock = new Mock<IMapper>();
        var currentUserMock = new Mock<ICurrentUser>();
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        var photoId = Guid.NewGuid();
        fileRepositoryMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileEntry?)null);

        var handler = new DeleteDocumentCommandHandler(
            fileServiceManagerMock.Object,
            fileRepositoryMock.Object,
            currentUserMock.Object,
            authorizationServiceMock.Object,
            publishEndpointMock.Object);

        var act = () => handler.Handle(new DeleteDocumentCommand { Id = photoId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void DeleteDocumentCommandValidator_Should_Validate_Id()
    {
        var validator = new DeleteDocumentCommandValidator();
        var validCommand = new DeleteDocumentCommand { Id = Guid.NewGuid() };
        var emptyCommand = new DeleteDocumentCommand { Id = Guid.Empty };

        validator.Validate(validCommand).IsValid.Should().BeTrue();
        validator.Validate(emptyCommand).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDocument_Should_Prune_All_WebP_Variants_From_Storage_When_Variants_Present()
    {
        var fileServiceManagerMock = new Mock<IFileServiceManager>();
        var fileRepositoryMock = new Mock<IMongoRepository<FileEntry, Guid>>();
        var currentUserMock = new Mock<ICurrentUser>();
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        var photoId = Guid.NewGuid();
        var existingFile = new FileEntry
        {
            Id = photoId,
            Name = "sunset_photo",
            FileName = "sunset_photo.jpg",
            FileLocation = "photos/sunset_photo.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            OwnerId = "user456",
            Deleted = false,
            Variants = new Dictionary<string, FileVariantInfo>
            {
                ["original"] = new() { VariantName = "original", StorageKey = "photos/sunset_photo_original.webp", Width = 1920, Height = 1080, Size = 40000 },
                ["medium"] = new() { VariantName = "medium", StorageKey = "photos/sunset_photo_medium.webp", Width = 1080, Height = 608, Size = 25000 },
                ["thumbnail"] = new() { VariantName = "thumbnail", StorageKey = "photos/sunset_photo_thumb.webp", Width = 256, Height = 144, Size = 5000 }
            }
        };

        fileRepositoryMock.Setup(r => r.FindByIdAsync(photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFile);

        currentUserMock.Setup(c => c.User).Returns(new ClaimsPrincipal());
        currentUserMock.Setup(c => c.CurrentUser).Returns(new UserProfile { PreferredUsername = "user456" });

        authorizationServiceMock.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var handler = new DeleteDocumentCommandHandler(
            fileServiceManagerMock.Object,
            fileRepositoryMock.Object,
            currentUserMock.Object,
            authorizationServiceMock.Object,
            publishEndpointMock.Object);

        await handler.Handle(new DeleteDocumentCommand { Id = photoId }, CancellationToken.None);

        existingFile.Deleted.Should().BeTrue();

        // 1 raw file + 3 WebP variants = 4 delete calls
        fileServiceManagerMock.Verify(f => f.DeleteAsync(It.IsAny<IFileEntry>(), It.IsAny<CancellationToken>()), Times.Exactly(4));

        publishEndpointMock.Verify(p => p.Publish(
            It.Is<PhotoDeletedIntegrationEvent>(e => e.PhotoId == photoId && e.StorageKey == "photos/sunset_photo.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MediaDbContext_Should_Create_Streamlined_Indexes_With_Sparse_FileLocation()
    {
        var mockCollection = new Mock<IMongoCollection<FileEntry>>();
        var mockIndexManager = new Mock<IMongoIndexManager<FileEntry>>();
        mockCollection.SetupGet(c => c.Indexes).Returns(mockIndexManager.Object);

        var optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<MongoOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new MongoOptions { ConnectionString = "mongodb://localhost:27017", DatabaseName = "test_media" });

        var dbContext = new VividImagination.Modules.Media.Persistence.MediaDbContext(optionsMock.Object)
        {
            FileEntries = mockCollection.Object
        };

        await dbContext.InitializeAsync(CancellationToken.None);

        mockIndexManager.Verify(i => i.CreateManyAsync(
            It.Is<IEnumerable<CreateIndexModel<FileEntry>>>(models =>
                models.Count() == 3 &&
                models.Any(m => m.Options.Name == "idx_fileLocation_unique" && m.Options.Unique == true && m.Options.Sparse == true) &&
                models.Any(m => m.Options.Name == "idx_ownerId_deleted_uploadedTime") &&
                models.Any(m => m.Options.Name == "idx_deleted_uploadedTime")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
