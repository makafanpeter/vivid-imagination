using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Modules.Media.Services.FileService.S3Files;

public class S3FileServiceManager : IFileServiceManager
{
    private readonly IAmazonS3? _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3FileServiceManager> _logger;

    public S3FileServiceManager(
        IOptions<ApplicationSettings> options,
        ILogger<S3FileServiceManager> logger,
        IAmazonS3? s3Client = null)
    {
        _logger = logger;
        _s3Client = s3Client;
        _bucketName = options.Value?.Storage?.S3?.BucketName ?? "vivid-imagination-photos";
    }

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        var key = $"{fileEntry.ContainerName?.Trim('/')}/{fileEntry.Id}{fileEntry.FileExtension}".TrimStart('/');

        if (_s3Client != null)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = fileEntry.ContentType ?? "application/octet-stream"
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
            _logger.LogInformation("Uploaded file {Id} to S3 bucket {Bucket} with key {Key}", fileEntry.Id, _bucketName, key);
        }
        else
        {
            _logger.LogWarning("IAmazonS3 client not configured; simulated S3 upload for key {Key}", key);
        }

        fileEntry.FileLocation = key;
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var key = fileEntry.FileLocation ?? $"{fileEntry.ContainerName?.Trim('/')}/{fileEntry.Id}{fileEntry.FileExtension}".TrimStart('/');

        if (_s3Client != null)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }

        _logger.LogWarning("IAmazonS3 client not configured; returning empty bytes for key {Key}", key);
        return [];
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var key = fileEntry.FileLocation ?? $"{fileEntry.ContainerName?.Trim('/')}/{fileEntry.Id}{fileEntry.FileExtension}".TrimStart('/');

        if (_s3Client != null)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("Deleted file {Key} from S3 bucket {Bucket}", key, _bucketName);
        }
        else
        {
            _logger.LogWarning("IAmazonS3 client not configured; simulated S3 deletion for key {Key}", key);
        }
    }
}
