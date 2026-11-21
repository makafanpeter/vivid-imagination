namespace VividImagination.Shared.Contract.Identity;

public static class UserIdExtensions
{
    public static Guid ToGuid(this long userId)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(userId).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    public static long ToLong(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        return BitConverter.ToInt64(bytes, 0);
    }
}
