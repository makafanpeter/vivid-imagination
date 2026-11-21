using MediatR;
using VividImagination.Modules.Media.Features.Photos.ViewModels;

namespace VividImagination.Modules.Media.Features.Photos.Queries;

public sealed record GetMyMediaQuery(
    int Page = 1,
    int PageSize = 20,
    string? OwnerId = null) : IRequest<IReadOnlyList<UploadDocumentViewModel>>;
