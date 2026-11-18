namespace VividImagination.Modules.Media.Services.FileService
{
    public interface IFileEntry
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string FileLocation { get; set; }
        public string? ContainerName { get; set; }

        public string Name { get; set; }
        public string ContentType { get; set; }

        public string Description { get; set; }
        public string OwnerId { get; set; }
        public string FileExtension { get; set; }

        public string Url { get; set; }
    }
}