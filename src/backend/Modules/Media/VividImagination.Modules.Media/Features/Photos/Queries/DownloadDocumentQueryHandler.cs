using MediatR;
using Microsoft.AspNetCore.Authorization;
using VividImagination.Modules.Media.Contracts;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Features.Photos.Queries
{
    public class DownloadDocumentQueryHandler(
        IFileServiceManager fileServiceManager,
        IMongoRepository<FileEntry, Guid> dbContext,
        ICurrentUser identityService,
        IAuthorizationService authorizationService)
        : IRequestHandler<DownloadDocumentQuery, FileViewModel>
    {
        public async Task<FileViewModel> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
        {
            var file = await dbContext.FindByIdAsync(request.Id, cancellationToken);

            if (file == null || file.Deleted)
            {
                throw new NotFoundException(nameof(FileEntry), request.Id);
            }
            
            var authResult = await authorizationService.AuthorizeAsync(
                (identityService.User ?? null)!,  
                file, 
                Operations.Read);

            if (!authResult.Succeeded)
            {
                throw new ForbiddenException(identityService.GetUserName());
            }

            IFileEntry targetFile = file;
            var contentType = file.ContentType;
            var fileName = file.FileName;

            if (!string.IsNullOrWhiteSpace(request.Variant) &&
                file.Variants != null &&
                file.Variants.TryGetValue(request.Variant.Trim().ToLowerInvariant(), out var variantInfo))
            {
                targetFile = new FileEntryModel
                {
                    Id = file.Id,
                    Name = file.Name,
                    ContainerName = file.ContainerName,
                    FileName = Path.GetFileName(variantInfo.StorageKey),
                    FileLocation = variantInfo.StorageKey,
                    ContentType = variantInfo.ContentType,
                    FileExtension = Path.GetExtension(variantInfo.StorageKey),
                    Size = variantInfo.Size
                };
                contentType = variantInfo.ContentType;
                fileName = Path.GetFileName(variantInfo.StorageKey);
            }

            var rawData = await fileServiceManager.ReadAsync(targetFile, cancellationToken);

            if (rawData == null)
            {
                throw new BadRequestException($"File is not available any more. Document GD={file.Id}");
            }
            var content = rawData;
            var response = new FileViewModel()
            {
                ContentType = contentType,
                Content = content,
                FileName = fileName
            };

            return response;
        }
    }
}