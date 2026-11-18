using VividImagination.Modules.Media.Persistence.Enums;

namespace VividImagination.Modules.Media.Services.Common;

public static class FileTypeHelper
{
    private static readonly Dictionary<string, StorageFileType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Image
        { ".jpg", StorageFileType.Image },
        { ".jpeg", StorageFileType.Image },
        { ".png", StorageFileType.Image },
        { ".gif", StorageFileType.Image },
        { ".bmp", StorageFileType.Image },
        { ".webp", StorageFileType.Image },
        { ".svg", StorageFileType.Image },
        { ".tiff", StorageFileType.Image },
        { ".tif", StorageFileType.Image },
        { ".ico", StorageFileType.Image },

        // Document
        { ".pdf", StorageFileType.Document },
        { ".doc", StorageFileType.Document },
        { ".docx", StorageFileType.Document },
        { ".txt", StorageFileType.Document },
        { ".rtf", StorageFileType.Document },
        { ".odt", StorageFileType.Document },

        // Spreadsheet
        { ".xls", StorageFileType.Spreadsheet },
        { ".xlsx", StorageFileType.Spreadsheet },
        { ".csv", StorageFileType.Spreadsheet },
        { ".ods", StorageFileType.Spreadsheet },

        // Presentation
        { ".ppt", StorageFileType.Presentation },
        { ".pptx", StorageFileType.Presentation },
        { ".odp", StorageFileType.Presentation },

        // Audio
        { ".mp3", StorageFileType.Audio },
        { ".wav", StorageFileType.Audio },
        { ".ogg", StorageFileType.Audio },
        { ".flac", StorageFileType.Audio },
        { ".aac", StorageFileType.Audio },
        { ".m4a", StorageFileType.Audio },

        // Video
        { ".mp4", StorageFileType.Video },
        { ".avi", StorageFileType.Video },
        { ".mkv", StorageFileType.Video },
        { ".mov", StorageFileType.Video },
        { ".wmv", StorageFileType.Video },
        { ".webm", StorageFileType.Video },
        { ".flv", StorageFileType.Video },

        // Archive
        { ".zip", StorageFileType.Archive },
        { ".rar", StorageFileType.Archive },
        { ".7z", StorageFileType.Archive },
        { ".tar", StorageFileType.Archive },
        { ".gz", StorageFileType.Archive },
        { ".bz2", StorageFileType.Archive }
    };

    public static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var trimmed = extension.Trim();
        return trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : $".{trimmed.ToLowerInvariant()}";
    }

    public static HashSet<string> NormalizeAllowedFiles(IEnumerable<string>? allowedFiles)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (allowedFiles == null)
        {
            return normalized;
        }

        foreach (var item in allowedFiles)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var ext = NormalizeExtension(part);
                if (!string.IsNullOrEmpty(ext))
                {
                    normalized.Add(ext);
                }
            }
        }

        return normalized;
    }

    public static StorageFileType DetectFileType(string? fileExtension, string? contentType = null)
    {
        var normalizedExt = NormalizeExtension(fileExtension);
        if (ExtensionMap.TryGetValue(normalizedExt, out var fileType))
        {
            return fileType;
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var mime = contentType.Trim().ToLowerInvariant();
            if (mime.StartsWith("image/"))
            {
                return StorageFileType.Image;
            }

            if (mime.StartsWith("audio/"))
            {
                return StorageFileType.Audio;
            }

            if (mime.StartsWith("video/"))
            {
                return StorageFileType.Video;
            }

            if (mime == "application/pdf" || mime.Contains("word") || mime == "text/plain")
            {
                return StorageFileType.Document;
            }

            if (mime.Contains("excel") || mime.Contains("spreadsheet") || mime == "text/csv")
            {
                return StorageFileType.Spreadsheet;
            }

            if (mime.Contains("powerpoint") || mime.Contains("presentation"))
            {
                return StorageFileType.Presentation;
            }

            if (mime.Contains("zip") || mime.Contains("compressed") || mime.Contains("tar") || mime.Contains("gzip"))
            {
                return StorageFileType.Archive;
            }
        }

        return StorageFileType.None;
    }

    public static bool IsAllowedCategory(StorageFileType allowedFlags, string? fileExtension, string? contentType = null)
    {
        if (allowedFlags == StorageFileType.All || allowedFlags == StorageFileType.None)
        {
            return true;
        }

        var detected = DetectFileType(fileExtension, contentType);
        if (detected == StorageFileType.None)
        {
            return false;
        }

        return (allowedFlags & detected) != 0;
    }

    public static bool IsAllowedExtension(IEnumerable<string>? allowedFiles, string? fileExtension)
    {
        var allowedSet = NormalizeAllowedFiles(allowedFiles);
        if (allowedSet.Count == 0)
        {
            return true;
        }

        var normalizedExt = NormalizeExtension(fileExtension);
        return allowedSet.Contains(normalizedExt);
    }
}
