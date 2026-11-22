using VividImagination.Mobile.Models;

namespace VividImagination.Mobile.Services;

public static class DisplayResolutionHelper
{
    /// <summary>
    /// Chooses the optimal photo variant for feed cards based on display screen width.
    /// Phones and smaller screens (&lt;= 600 dp) use Medium; larger screens/desktop use Original.
    /// </summary>
    public static PhotoVariant GetFeedVariant()
    {
        try
        {
            var display = DeviceDisplay.Current.MainDisplayInfo;
            var density = display.Density > 0 ? display.Density : 1;
            var widthDp = display.Width / density;

            return widthDp > 600 ? PhotoVariant.Original : PhotoVariant.Medium;
        }
        catch
        {
            return PhotoVariant.Medium;
        }
    }

    /// <summary>
    /// Chooses the optimal photo variant based on explicit width in density-independent pixels.
    /// </summary>
    public static PhotoVariant GetVariantForWidth(double widthDp)
    {
        if (widthDp <= 256) return PhotoVariant.Thumbnail;
        if (widthDp <= 600) return PhotoVariant.Medium;
        return PhotoVariant.Original;
    }
}
