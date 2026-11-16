namespace VividImagination.Shared.Infrastructure.ComponentModel
{
    /// <summary>
    /// Reader/Write locker type
    /// </summary>
    public enum ReaderWriteLockType
    {
        Read,
        Write,
        UpgradeableRead
    }
}
