using System.Windows.Media;

namespace TokenOmamoriTool;

/// <summary>
/// The omamori color palette (spec v0.3追補2 §12.2) — the single source of truth for every
/// status/background color in the app. XAML consumes these via Resources/Theme.xaml (x:Static),
/// code consumes the brushes directly, and tools/generate-icons.ps1 extracts the *Hex constants
/// by regex, so keep them as plain `const string` literals with the exact `#RRGGBB` form.
/// Plain C# (no Application/pack URI) so unit tests can touch the brushes without WPF bootstrap.
/// </summary>
public static class OmamoriTheme
{
    // 常盤色 — normal status (bag fill, bars below 70%).
    public const string HealthyHex = "#1B813E";
    // 琥珀色 — warning status (70%+: bars, menu items, tray/bag fill).
    public const string WarningHex = "#CA7A2C";
    // 紅緋系 — 90%+ danger tier on bars/menus. Not part of the §12.2 table (which only defines the
    // 2-state tray colors) but kept so the bars retain their existing 3-step severity scale;
    // managed here so it stays out of individual screens all the same.
    public const string DangerHex = "#C73E3A";
    // 金茶 — cord loop, knot, borders/accents.
    public const string AccentHex = "#B98C4A";
    // 生成り(和紙) — window backgrounds.
    public const string WashiHex = "#F8F4E6";

    public static readonly Color HealthyColor = Parse(HealthyHex);
    public static readonly Color WarningColor = Parse(WarningHex);
    public static readonly Color DangerColor = Parse(DangerHex);
    public static readonly Color AccentColor = Parse(AccentHex);
    public static readonly Color WashiColor = Parse(WashiHex);

    public static readonly SolidColorBrush HealthyBrush = Freeze(HealthyColor);
    public static readonly SolidColorBrush WarningBrush = Freeze(WarningColor);
    public static readonly SolidColorBrush DangerBrush = Freeze(DangerColor);
    public static readonly SolidColorBrush AccentBrush = Freeze(AccentColor);
    public static readonly SolidColorBrush WashiBrush = Freeze(WashiColor);

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
