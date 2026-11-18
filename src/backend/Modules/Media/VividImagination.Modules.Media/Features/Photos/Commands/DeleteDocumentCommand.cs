using MediatR;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class DeleteDocumentCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
    }
}