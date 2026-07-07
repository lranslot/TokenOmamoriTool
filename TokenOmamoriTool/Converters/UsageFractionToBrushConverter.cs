using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TokenOmamoriTool.Converters;

public class UsageFractionToBrushConverter : IValueConverter
{
    // Colors come from the shared omamori palette (spec §12.2) — only the threshold→brush
    // mapping lives here.
    public static readonly SolidColorBrush OkBrush = OmamoriTheme.HealthyBrush;
    public static readonly SolidColorBrush WarnBrush = OmamoriTheme.WarningBrush;
    public static readonly SolidColorBrush DangerBrush = OmamoriTheme.DangerBrush;

    public static SolidColorBrush BrushFor(double usageFraction)
    {
        if (usageFraction >= 0.9) return DangerBrush;
        if (usageFraction >= 0.7) return WarnBrush;
        return OkBrush;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = value is double d ? d : 0.0;
        return BrushFor(fraction);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
