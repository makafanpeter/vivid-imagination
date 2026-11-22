using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Features.Photos.Queries
{


    public class GetDocumentQueryHandler(
        IMongoRepository<FileEntry, Guid> dbContext,
        IMapper mapper,
        ICurrentUser identityService,
        IAuthorizationService authorizationService)
        : IRequestHandler<GetDocumentQuery, UploadDocumentViewModel>
    {
      


        public async Task<UploadDocumentViewModel> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
        {
            var file = await dbContext.FindByIdAsync(request.Id, cancellationToken);


            if (file == null || file.Deleted)
            {
                throw new NotFoundException(nameof(FileEntry), request.Id);
            }
            
            var authorizationResult = await authorizationService.AuthorizeAsync((identityService.User ?? null)!, file, Operations.Read);
            if (!authorizationResult.Succeeded)
            {
               throw new ForbiddenException(identityService.GetUserName());
            }

            var fileEntry = mapper.Map<UploadDocumentViewModel>(file);
           

            return fileEntry;
        }
    }
}
