namespace VividImagination.Modules.Media.Services;

public static class BlurHashGenerator
{
    // High-quality default visual blurhash placeholders
    private static readonly string[] DefaultHashes =
    [
        "L6PZfSi_.AyE_3t7t7R**0o#DgR4",
        "LEHV6nWB2yk8pyoJadR*.7kCMdnj",
        "LGF5?xYk^6#M@-5cSimple9F$M_Io",
        "LKO2?U%2Tw=w]~RBVZRi};RPxuwH"
    ];

    public static string Generate(string? seed = null)
    {
        if (string.IsNullOrEmpty(seed))
        {
            return DefaultHashes[0];
        }

        var index = Math.Abs(seed.GetHashCode()) % DefaultHashes.Length;
        return DefaultHashes[index];
    }
}
