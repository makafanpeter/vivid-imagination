namespace VividImagination.Modules.Media.Contracts;

public class FileViewModel
{
    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required byte[] Content { get; set; }
}