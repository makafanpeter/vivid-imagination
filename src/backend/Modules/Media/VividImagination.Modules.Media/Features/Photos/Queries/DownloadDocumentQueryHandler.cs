using MediatR;
using Microsoft.AspNetCore.Authorization;
using VividImagination.Modules.Media.Contracts;
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
        IMongoRepository<FileEntry, Guid> dynamoDbContext,
        ICurrentUser identityService,
        IAuthorizationService authorizationService)
        : IRequestHandler<DownloadDocumentQuery, FileViewModel>
    {
        public async Task<FileViewModel> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
        {
            var file = await dynamoDbContext.FindByIdAsync(request.Id,cancellationToken);


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
            

            var rawData = await fileServiceManager.ReadAsync(file, cancellationToken);

            if (rawData == null)
            {
                throw new BadRequestException($"File is not available any more. Document GD={file.Id}");
            }
            var content= rawData;
            var response = new FileViewModel()
            {
                ContentType = file.ContentType,
                Content = content,
                FileName = file.FileName
            };

            return response;
        }
    }
}