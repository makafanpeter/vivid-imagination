using MediatR;
using VividImagination.Modules.Media.Contracts;

namespace VividImagination.Modules.Media.Features.Photos.Queries
{
    public class DownloadDocumentQuery : IRequest<FileViewModel>
    {
        public Guid Id { get; set; }
    }


}