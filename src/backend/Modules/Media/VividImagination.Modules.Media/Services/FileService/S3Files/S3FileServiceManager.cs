using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Modules.Media.Services.FileService.S3Files;

public class S3FileServiceManager : IFileServiceManager
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3FileServiceManager> _logger;
    private readonly RegionEndpoint _region;

    public S3FileServiceManager(
        IOptions<ApplicationSettings> options,
        ILogger<S3FileServiceManager> logger)
    {
        var settings = options.Value.Storage.S3
            ?? throw new InvalidOperationException("S3 storage settings are not configured.");

        _logger = logger;
        _bucketName = settings.BucketName;
        _region = RegionEndpoint.GetBySystemName(settings.Region);
        _s3Client = new AmazonS3Client(_region);
    }

    public async Task CreateAsync(
        IFileEntry fileEntry,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var key = BuildObjectKey(fileEntry);

        using var transferUtility = new TransferUtility(_s3Client);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = fileEntry.ContentType ?? "application/octet-stream",
            CannedACL = S3CannedACL.NoACL
        };

        AddMetadata(uploadRequest, fileEntry);

        await transferUtility.UploadAsync(uploadRequest, cancellationToken);

        fileEntry.FileLocation = key;
        fileEntry.Url = BuildPublicUrl(key);
        fileEntry.BucketName = _bucketName;

        _logger.LogInformation(
            "Uploaded file {FileId} to S3 bucket {BucketName} with key {ObjectKey}",
            fileEntry.Id,
            _bucketName,
            key);
    }

    public async Task<byte[]> ReadAsync(
        IFileEntry fileEntry,
        CancellationToken cancellationToken = default)
    {
        var key = fileEntry.FileLocation ?? BuildObjectKey(fileEntry);

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        using var response = await _s3Client.GetObjectAsync(
            request,
            cancellationToken);

        await using var memoryStream = new MemoryStream();

        await response.ResponseStream.CopyToAsync(
            memoryStream,
            cancellationToken);

        return memoryStream.ToArray();
    }

    public async Task DeleteAsync(
        IFileEntry fileEntry,
        CancellationToken cancellationToken = default)
    {
        var key = fileEntry.FileLocation ?? BuildObjectKey(fileEntry);

        await _s3Client.DeleteObjectAsync(
            _bucketName,
            key,
            cancellationToken);

        _logger.LogInformation(
            "Deleted file {ObjectKey} from S3 bucket {BucketName}",
            key,
            _bucketName);
    }

    private static string BuildObjectKey(IFileEntry fileEntry)
    {
        var container = fileEntry.ContainerName?.Trim('/');

        var key = string.IsNullOrWhiteSpace(container)
            ? $"{fileEntry.Id}{fileEntry.FileExtension}"
            : $"{container}/{fileEntry.Id}{fileEntry.FileExtension}";

        return key.TrimStart('/');
    }

    private static void AddMetadata(
        TransferUtilityUploadRequest request,
        IFileEntry fileEntry)
    {
        if (!string.IsNullOrWhiteSpace(fileEntry.FileExtension))
        {
            request.Metadata.Add("file-extension", fileEntry.FileExtension);
        }

        if (!string.IsNullOrWhiteSpace(fileEntry.FileName))
        {
            request.Metadata.Add("file-name", fileEntry.FileName);
        }

        if (!string.IsNullOrWhiteSpace(fileEntry.Description))
        {
            request.Metadata.Add("description", fileEntry.Description);
        }

        if (!string.IsNullOrWhiteSpace(fileEntry.Name))
        {
            request.Metadata.Add("name", fileEntry.Name);
        }
    }

    private string BuildPublicUrl(string key)
    {
        return $"https://{_bucketName}.s3.{_region.SystemName}.amazonaws.com/{Uri.EscapeDataString(key)}";
    }
}