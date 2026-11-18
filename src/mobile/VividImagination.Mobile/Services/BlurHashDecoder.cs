namespace VividImagination.Mobile.Services;

public static class BlurHashDecoder
{
    public static ImageSource? DecodeToImageSource(string blurHash, int width = 32, int height = 32, double punch = 1.0)
    {
        if (string.IsNullOrWhiteSpace(blurHash) || blurHash.Length < 6)
        {
            return null;
        }

        try
        {
            var pixels = BlurHashCore.Decode(blurHash, width, height, punch);
            if (pixels == null)
            {
                return null;
            }

            var bmpBytes = BlurHashCore.CreateBmp(pixels, width, height);
            return ImageSource.FromStream(() => new MemoryStream(bmpBytes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BlurHash decode error: {ex.Message}");
            return null;
        }
    }

    public static byte[]? Decode(string blurHash, int width, int height, double punch = 1.0) =>
        BlurHashCore.Decode(blurHash, width, height, punch);
}
