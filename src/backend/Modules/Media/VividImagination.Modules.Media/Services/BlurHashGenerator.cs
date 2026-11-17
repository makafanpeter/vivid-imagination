using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VividImagination.Modules.Media.Services;

public static class BlurHashGenerator
{
    private const string Base83Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~";

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

    /// <summary>
    /// Computes a standard BlurHash string directly from image pixels using 4x3 DCT.
    /// </summary>
    public static string Encode(Image<Rgb24> image, int componentsX = 4, int componentsY = 3)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (componentsX < 1 || componentsX > 9 || componentsY < 1 || componentsY > 9)
        {
            throw new ArgumentException("Components X and Y must be between 1 and 9.");
        }

        int width = image.Width;
        int height = image.Height;

        var factors = new (double R, double G, double B)[componentsX * componentsY];

        // Precompute linear RGB values for all pixels
        var linearR = new double[width, height];
        var linearG = new double[width, height];
        var linearB = new double[width, height];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    linearR[x, y] = SrgbToLinear(row[x].R);
                    linearG[x, y] = SrgbToLinear(row[x].G);
                    linearB[x, y] = SrgbToLinear(row[x].B);
                }
            }
        });

        for (int j = 0; j < componentsY; j++)
        {
            for (int i = 0; i < componentsX; i++)
            {
                double normalisation = (i == 0 && j == 0) ? 1.0 : 2.0;
                double r = 0, g = 0, b = 0;

                for (int y = 0; y < height; y++)
                {
                    double cosY = Math.Cos(Math.PI * j * y / height);
                    for (int x = 0; x < width; x++)
                    {
                        double basis = Math.Cos(Math.PI * i * x / width) * cosY;
                        r += basis * linearR[x, y];
                        g += basis * linearG[x, y];
                        b += basis * linearB[x, y];
                    }
                }

                double scale = normalisation / (width * height);
                factors[j * componentsX + i] = (r * scale, g * scale, b * scale);
            }
        }

        var sb = new StringBuilder();

        // 1. Size flag (componentsX - 1) + (componentsY - 1) * 9
        int sizeFlag = (componentsX - 1) + (componentsY - 1) * 9;
        EncodeBase83(sizeFlag, 1, sb);

        // 2. Maximum AC component value
        double maximumAcComponent = 0;
        if (componentsX * componentsY > 1)
        {
            for (int i = 1; i < componentsX * componentsY; i++)
            {
                maximumAcComponent = Math.Max(maximumAcComponent, Math.Abs(factors[i].R));
                maximumAcComponent = Math.Max(maximumAcComponent, Math.Abs(factors[i].G));
                maximumAcComponent = Math.Max(maximumAcComponent, Math.Abs(factors[i].B));
            }

            int quantisedMaximumValue = (int)Math.Max(0, Math.Min(82, Math.Floor(maximumAcComponent * 166.0 - 0.5)));
            maximumAcComponent = (quantisedMaximumValue + 1) / 166.0;
            EncodeBase83(quantisedMaximumValue, 1, sb);
        }
        else
        {
            EncodeBase83(0, 1, sb);
        }

        // 3. DC component (average color)
        int dcR = LinearToSrgb(factors[0].R);
        int dcG = LinearToSrgb(factors[0].G);
        int dcB = LinearToSrgb(factors[0].B);
        EncodeBase83((dcR << 16) + (dcG << 8) + dcB, 4, sb);

        // 4. AC components
        for (int i = 1; i < componentsX * componentsY; i++)
        {
            int quantR = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(factors[i].R / maximumAcComponent, 0.5) * 9.0 + 9.5)));
            int quantG = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(factors[i].G / maximumAcComponent, 0.5) * 9.0 + 9.5)));
            int quantB = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(factors[i].B / maximumAcComponent, 0.5) * 9.0 + 9.5)));

            int quantised = quantR * 19 * 19 + quantG * 19 + quantB;
            EncodeBase83(quantised, 2, sb);
        }

        return sb.ToString();
    }

    private static void EncodeBase83(int value, int length, StringBuilder sb)
    {
        for (int i = 1; i <= length; i++)
        {
            int digit = (value / (int)Math.Pow(83, length - i)) % 83;
            sb.Append(Base83Chars[digit]);
        }
    }

    private static double SrgbToLinear(int value)
    {
        double v = value / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static int LinearToSrgb(double value)
    {
        double v = Math.Max(0.0, Math.Min(1.0, value));
        double s = v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
        return (int)Math.Round(s * 255.0);
    }

    private static double SignPow(double val, double exp)
    {
        return Math.Sign(val) * Math.Pow(Math.Abs(val), exp);
    }
}
