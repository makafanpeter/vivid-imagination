using VividImagination.Modules.Media.Services.FileService;

namespace VividImagination.Modules.Media.Features.Photos.ViewModels
{
    public class FileEntryModel: IFileEntry
    {
        public Guid Id { get; set; }

        public string  Name { get; set; }  =string.Empty;
        public string ContentType { get; set; } =string.Empty;

        public string? Description { get; set; } =string.Empty;

        public long Size { get; set; }

        public DateTimeOffset UploadedTime { get; set; }

        public string FileName { get; set; } =string.Empty;

        public string FileLocation { get; set; } =string.Empty;
        public string? ContainerName { get; set; }
        
        public string? OwnerId { get; set; }
        public string FileExtension { get; set; }=string.Empty;
        public string Url { get; set; } =string.Empty;
        public string SourceIp { get; set; } =string.Empty;
    }
}