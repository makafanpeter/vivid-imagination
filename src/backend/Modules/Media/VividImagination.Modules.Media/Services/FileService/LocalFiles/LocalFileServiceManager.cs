using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Media.Services.FileService.LocalFiles;

public class LocalFileServiceManager : IFileServiceManager
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileServiceManager> _logger;

    public LocalFileServiceManager(
        IOptions<ApplicationSettings> options,
        ILogger<LocalFileServiceManager> logger)
    {
        _logger = logger;
        _storagePath = options.Value.Storage.Local.Path 
                       ?? throw new InvalidOperationException("Local storage path is not configured");
        
        // Ensure directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created local storage directory: {Path}", _storagePath);
        }
    }


    public async Task CreateAsync(
        IFileEntry fileEntry,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        // Decode and normalize any percent-encoded characters and path separators
        // before path construction so traversal attacks are caught cross-platform.
        var containerName = NormalizePath(fileEntry.ContainerName);

        var fileName = $"{fileEntry.Id}{fileEntry.FileExtension}";
        var containerPath = string.IsNullOrEmpty(containerName)
            ? _storagePath
            : Path.Combine(_storagePath, containerName);

        var resolvedContainer = Path.GetFullPath(containerPath);
        var resolvedRoot = Path.GetFullPath(_storagePath);

        if (!resolvedContainer.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && resolvedContainer != resolvedRoot)
        {
            _logger.LogWarning(
                "Container traversal attempt blocked. ContainerName: {ContainerName}, ResolvedPath: {ResolvedPath}",
                fileEntry.ContainerName, resolvedContainer);
            throw new BadRequestException($"Invalid container name: '{fileEntry.ContainerName}'.");
        }

        Directory.CreateDirectory(resolvedContainer);

        var filePath = Path.Combine(resolvedContainer, fileName);
        await using var outputStream = File.Create(filePath);
        await stream.CopyToAsync(outputStream, cancellationToken);

        fileEntry.FileLocation = filePath;
        _logger.LogInformation("File saved to local storage: {FileName} at {Path}", fileName, filePath);
    }

    public Task DeleteAsync(
        IFileEntry fileEntry,
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(fileEntry);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted from local storage: {FileId}", fileEntry.Id);
        }
        else
        {
            _logger.LogWarning("Delete called but file not found: {Path}", filePath);
        }

        return Task.CompletedTask;
    }
    
    private string ResolveFilePath(IFileEntry fileEntry)
    {
        var resolvedRoot = Path.GetFullPath(_storagePath);

        string filePath;

        if (!string.IsNullOrEmpty(fileEntry.FileLocation))
        {
            var normalizedLocation = NormalizePath(fileEntry.FileLocation);
            filePath = Path.GetFullPath(normalizedLocation);
        }
        else
        {
            var containerName = NormalizePath(fileEntry.ContainerName);
            var fileName = $"{fileEntry.Id}{fileEntry.FileExtension}";

            filePath = string.IsNullOrEmpty(containerName)
                ? Path.Combine(_storagePath, fileName)
                : Path.Combine(_storagePath, containerName, fileName);

            filePath = Path.GetFullPath(filePath);
        }

        if (!filePath.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && filePath != resolvedRoot)
        {
            _logger.LogWarning(
                "Path traversal attempt blocked in ResolveFilePath. FileId: {FileId}, ResolvedPath: {FilePath}",
                fileEntry.Id, filePath);
            throw new BadRequestException($"Invalid file path for file '{fileEntry.Id}'.");
        }

        return filePath;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var current = path;
        var previous = string.Empty;
        while (current.Contains('%') && current != previous)
        {
            previous = current;
            current = Uri.UnescapeDataString(current);
        }

        return current
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(fileEntry);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {fileEntry.Id}");

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }
    
}