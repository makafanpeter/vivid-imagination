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

public sealed class SelectedSegmentBackgroundConverter : IValueConverter
{
    private static readonly Color SelectedBg = Color.FromArgb("#FF551C");
    private static readonly Color UnselectedBg = Colors.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? SelectedBg : UnselectedBg;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class SelectedSegmentTextColorConverter : IValueConverter
{
    private static readonly Color SelectedText = Colors.White;
    private static readonly Color UnselectedText = Color.FromArgb("#C9C0B5");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? SelectedText : UnselectedText;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
