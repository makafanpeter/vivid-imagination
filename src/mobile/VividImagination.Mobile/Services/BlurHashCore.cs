namespace VividImagination.Mobile.Services;

public static class BlurHashCore
{
    private const string Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~";

    public static byte[]? Decode(string blurHash, int width, int height, double punch = 1.0)
    {
        if (string.IsNullOrWhiteSpace(blurHash) || blurHash.Length < 6)
        {
            return null;
        }

        var numCompEnc = Decode83(blurHash.Substring(0, 1));
        var numCompX = (numCompEnc % 9) + 1;
        var numCompY = (numCompEnc / 9) + 1;

        if (blurHash.Length != 4 + 2 * numCompX * numCompY)
        {
            return null;
        }

        var maxAcEnc = Decode83(blurHash.Substring(1, 1));
        var maxAc = (maxAcEnc + 1) / 166.0;

        var colors = new (double r, double g, double b)[numCompX * numCompY];

        var averageColorEnc = Decode83(blurHash.Substring(2, 4));
        colors[0] = DecodeDc(averageColorEnc);

        for (var i = 1; i < colors.Length; i++)
        {
            var acEnc = Decode83(blurHash.Substring(4 + i * 2, 2));
            colors[i] = DecodeAc(acEnc, maxAc * punch);
        }

        // BGRA format
        var bytes = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                double r = 0, g = 0, b = 0;

                for (var j = 0; j < numCompY; j++)
                {
                    var basisY = Math.Cos(Math.PI * y * j / height);
                    for (var i = 0; i < numCompX; i++)
                    {
                        var basisX = Math.Cos(Math.PI * x * i / width);
                        var basis = basisX * basisY;
                        var color = colors[i + j * numCompX];
                        r += color.r * basis;
                        g += color.g * basis;
                        b += color.b * basis;
                    }
                }

                var pixelIndex = (y * width + x) * 4;
                bytes[pixelIndex + 0] = (byte)LinearToSRgb(b); // Blue
                bytes[pixelIndex + 1] = (byte)LinearToSRgb(g); // Green
                bytes[pixelIndex + 2] = (byte)LinearToSRgb(r); // Red
                bytes[pixelIndex + 3] = 255;                  // Alpha
            }
        }

        return bytes;
    }

    public static byte[] CreateBmp(byte[] bgraPixels, int width, int height)
    {
        const int fileHeaderSize = 14;
        const int dibHeaderSize = 40;
        var imageSize = bgraPixels.Length;
        var fileSize = fileHeaderSize + dibHeaderSize + imageSize;

        var bmp = new byte[fileSize];
        using var stream = new MemoryStream(bmp);
        using var writer = new BinaryWriter(stream);

        // BITMAPFILEHEADER
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0); // reserved1
        writer.Write((short)0); // reserved2
        writer.Write(fileHeaderSize + dibHeaderSize); // offset to pixels

        // BITMAPINFOHEADER
        writer.Write(dibHeaderSize);
        writer.Write(width);
        writer.Write(-height); // negative height for top-down DIB
        writer.Write((short)1); // planes
        writer.Write((short)32); // 32 bits per pixel (BGRA)
        writer.Write(0); // BI_RGB (uncompressed)
        writer.Write(imageSize);
        writer.Write(2835); // horizontal resolution (pixels/meter)
        writer.Write(2835); // vertical resolution
        writer.Write(0); // colors in palette
        writer.Write(0); // important colors

        // Pixel data
        writer.Write(bgraPixels);

        return bmp;
    }

    private static int Decode83(string str)
    {
        var value = 0;
        foreach (var c in str)
        {
            var digit = Characters.IndexOf(c);
            if (digit == -1) return 0;
            value = value * 83 + digit;
        }
        return value;
    }

    private static (double r, double g, double b) DecodeDc(int value)
    {
        var r = (value >> 16) & 255;
        var g = (value >> 8) & 255;
        var b = value & 255;
        return (SRgbToLinear(r), SRgbToLinear(g), SRgbToLinear(b));
    }

    private static (double r, double g, double b) DecodeAc(int value, double maxAc)
    {
        var quantR = value / (19 * 19);
        var quantG = (value / 19) % 19;
        var quantB = value % 19;

        return (
            SignPow((quantR - 9) / 9.0, 2.0) * maxAc,
            SignPow((quantG - 9) / 9.0, 2.0) * maxAc,
            SignPow((quantB - 9) / 9.0, 2.0) * maxAc
        );
    }

    private static double SignPow(double val, double exp) =>
        Math.Sign(val) * Math.Pow(Math.Abs(val), exp);

    private static double SRgbToLinear(int value)
    {
        var v = value / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static int LinearToSRgb(double value)
    {
        var v = Math.Clamp(value, 0.0, 1.0);
        var s = v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
        return (int)Math.Round(Math.Clamp(s * 255.0, 0, 255));
    }
}
