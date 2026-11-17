using MediatR;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Enums;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class UploadDocumentCommand : IRequest<UploadDocumentViewModel>
    {
        public required string Name { get; set; }
        public required Stream ContentStream { get; set; }
        public required string ContentType { get; set; }
        public long ContentLength { get; set; }

        public required string FileName { get; set; }

        public required string Description { get; set; }
        public bool Encrypt { get; set; }
        public required string FileExtension { get; set; }
        public string? ContainerName { get; set; }
        public bool Public { get; set; }

        public StorageFileType FileTypes { get; set; } = StorageFileType.All;
        public string[]? AllowedFiles { get; set; }
    }
}
