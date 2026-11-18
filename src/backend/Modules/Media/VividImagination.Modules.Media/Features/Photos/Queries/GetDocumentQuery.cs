using MediatR;
using VividImagination.Modules.Media.Features.Photos.ViewModels;

namespace VividImagination.Modules.Media.Features.Photos.Queries
{
    public class GetDocumentQuery:IRequest<UploadDocumentViewModel>
    {
        public Guid Id { get; set; }
    }
}
