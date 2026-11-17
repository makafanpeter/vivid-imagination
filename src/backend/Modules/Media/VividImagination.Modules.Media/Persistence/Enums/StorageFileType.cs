namespace VividImagination.Modules.Media.Persistence.Enums;

[Flags]
public enum StorageFileType
{
    None = 0,
    Image = 1 << 0,
    Document = 1 << 1,
    Spreadsheet = 1 << 2,
    Presentation = 1 << 3,
    Audio = 1 << 4,
    Video = 1 << 5,
    Archive = 1 << 6,
    All = Image | Document | Spreadsheet | Presentation | Audio | Video | Archive
}
