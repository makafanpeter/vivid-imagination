namespace VividImagination.Modules.Media.Services.FileService
{
    public interface IFileServiceManager
    {
        Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default);

        Task<byte[]> ReadAsync(IFileEntry fileEntry,  CancellationToken cancellationToken = default);

        Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default);
    }




}
