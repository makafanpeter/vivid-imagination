using System.Globalization;

namespace VividImagination.Mobile.Converters;

public sealed class AccentVermilionOrMutedConverter : IValueConverter
{
    private static readonly Color AccentColor = Color.FromArgb("#FF551C");
    private static readonly Color MutedColor = Color.FromArgb("#A89F95");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? AccentColor : MutedColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class DestructiveRedOrMutedConverter : IValueConverter
{
    private static readonly Color RedColor = Color.FromArgb("#EF4444");
    private static readonly Color MutedColor = Color.FromArgb("#A89F95");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? RedColor : MutedColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
