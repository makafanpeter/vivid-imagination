using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Media.Controllers.v1;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Helpers;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class UploadDocumentCommandHandler(
        IFileServiceManager fileServiceManager,
        IHttpContextAccessor accessor,
        IMongoRepository<FileEntry, Guid> fileRepository,
        IMapper mapper,
        ICurrentUser identityService,
        LinkGenerator linkGenerator,
        IWebHelper webHelper,
        IPublishEndpoint publishEndpoint)
        : IRequestHandler<UploadDocumentCommand, UploadDocumentViewModel>
    {
        public async Task<UploadDocumentViewModel> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var appReference = identityService.GetUserId();
            var model = new FileEntryModel()
            {
                Description = request.Description,
                FileName = request.FileName,
                Name = request.Name,
                Id = id,
                Size = request.ContentLength,
                UploadedTime = now,
                ContainerName = request.ContainerName,
                OwnerId = appReference.ToString(),
                ContentType = request.ContentType,
                FileExtension = request.FileExtension,
                SourceIp = webHelper.GetCurrentIpAddress(),
            };

            await using var memoryStream = request.ContentStream;
            await fileServiceManager.CreateAsync(model, memoryStream, cancellationToken);

            string? url = null;
            if (accessor.HttpContext != null)
            {
                var enforceSsl = webHelper.IsCurrentConnectionSecured();
                var scheme = enforceSsl ? "https" : accessor.HttpContext.Request.Scheme;
                url = linkGenerator.GetUriByName(
                    accessor.HttpContext!,
                    endpointName: nameof(MediaApis.DownloadEndpointName),
                    values: new { id },
                    scheme: scheme,
                    host: accessor.HttpContext!.Request.Host,
                    pathBase: accessor.HttpContext.Request.PathBase);
            }

            var entry = new FileEntry()
            {
                Id = id,
                UploadedTime = now.UtcDateTime,
                Description = model.Description,
                FileLocation = model.FileLocation,
                FileName = model.FileName,
                Name = model.Name,
                Size = request.ContentLength,
                ContainerName = model.ContainerName,
                FileExtension = request.FileExtension,
                ContentType = request.ContentType,
                OwnerId = appReference.ToString(),
                Url = url,
                SourceIp = model.SourceIp,
                CreatedOn = DateTime.UtcNow,
            };

            await fileRepository.AddAsync(entry, cancellationToken);

            // Generate blur hash and publish PhotoUploadedIntegrationEvent for Moderation and Feed
            var blurHash = BlurHashGenerator.Generate(model.FileName);
            var userId = Guid.Empty;
            if (appReference.HasValue)
            {
                var bytes = new byte[16];
                BitConverter.GetBytes(appReference.Value).CopyTo(bytes, 0);
                userId = new Guid(bytes);
            }

            await publishEndpoint.Publish(new PhotoUploadedIntegrationEvent(
                entry.Id,
                userId,
                entry.FileLocation ?? entry.FileName,
                blurHash,
                entry.Description ?? entry.Name ?? string.Empty,
                entry.UploadedTime), cancellationToken);

            var response = mapper.Map<UploadDocumentViewModel>(entry);
            return response;
        }
    }
}
