using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class DeleteDocumentCommandHandler(
        IFileServiceManager fileServiceManager,
        IMongoRepository<FileEntry, Guid> fileRepository,
        IMapper mapper,
        ICurrentUser identityService,
        IAuthorizationService authorizationService)
        : IRequestHandler<DeleteDocumentCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var file = await fileRepository.FindByIdAsync(request.Id, cancellationToken);


            if (file == null || file.Deleted)
            {
                throw new NotFoundException(nameof(FileEntry), request.Id);
            }
            var authResult = await authorizationService.AuthorizeAsync(
                (identityService.User ?? null)!,  
                file, 
                Operations.Delete);

            if (!authResult.Succeeded)
            {
                throw new ForbiddenException(identityService.CurrentUser?.PreferredUsername ?? string.Empty);
            }
            
            await fileServiceManager.DeleteAsync(file, cancellationToken);

            file.Deleted = true;

            await  fileRepository.AddAsync(file, cancellationToken);
            

            return Unit.Value;
        }
    }
}