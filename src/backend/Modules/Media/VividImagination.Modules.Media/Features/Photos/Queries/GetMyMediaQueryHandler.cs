using AutoMapper;
using MediatR;
using MongoDB.Driver;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Media.Features.Photos.Queries;

public class GetMyMediaQueryHandler(
    IMongoRepository<FileEntry, Guid> fileRepository,
    IMapper mapper,
    ICurrentUser identityService)
    : IRequestHandler<GetMyMediaQuery, IReadOnlyList<UploadDocumentViewModel>>
{
    public async Task<IReadOnlyList<UploadDocumentViewModel>> Handle(GetMyMediaQuery request, CancellationToken cancellationToken)
    {
        var ownerId = request.OwnerId
            ?? identityService.GetUserId()?.ToString()
            ?? identityService.CurrentUser?.Sub
            ?? throw new UnAuthorizedException("You are not authorized");

        var pageIndex = Math.Max(0, request.Page - 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var sort = Builders<FileEntry>.Sort
            .Descending(x => x.CreatedOn);

        var pagedItems = await fileRepository.FindPagedAsync(
            x => !x.Deleted && x.OwnerId == ownerId,
            pageIndex,
            pageSize,
            sort,
            cancellationToken);

        return [.. pagedItems.Select(mapper.Map<UploadDocumentViewModel>)];
    }
}
