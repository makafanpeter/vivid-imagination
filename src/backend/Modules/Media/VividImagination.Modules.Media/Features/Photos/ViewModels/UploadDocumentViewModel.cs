using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Application.Mapping;

namespace VividImagination.Modules.Media.Features.Photos.ViewModels
{
    public class UploadDocumentViewModel: IMapFrom<FileEntry>
    {
        public Guid Id { get; set; }
        public required string FileName { get; set; }
        public string? ContainerName { get; set; }
        public string? Name { get; set; }
        public required string ContentType { get; set; }
        public string? Description { get; set; }
        public bool Encrypted { get; set; }
        public string? OwnerId { get; set; }
        public required string FileExtension { get; set; }
        public string? Url { get; set; }
        public bool Public { get; set; }
        public string? OrganizationId { get; set; }
        public string Status { get; set; } = "Processing";
        public string? BlurHash { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Dictionary<string, FileVariantInfo> Variants { get; set; } = new();
    }
}
