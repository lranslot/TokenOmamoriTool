using System.Globalization;
using System.Text.RegularExpressions;

namespace TokenOmamoriTool.Services;

// `rtk gain` only ever gives us its own human-rounded count (e.g. "2.2M"), never a raw integer, so
// computing a same-day delta requires parsing that rounded string back into an approximate number
// and re-formatting it the same way. This is inherently approximate — see docs/実装メモ_アーキテクチャ詳細.md.
public static class TokenCountFormatter
{
    private static readonly Regex CountPattern = new(@"^([\d,]*\.?\d+)\s*([KMB]?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static double? TryParse(string text)
    {
        var match = CountPattern.Match(text.Trim());
        if (!match.Success) return null;
        if (!double.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        return match.Groups[2].Value.ToUpperInvariant() switch
        {
            "K" => number * 1_000,
            "M" => number * 1_000_000,
            "B" => number * 1_000_000_000,
            _ => number,
        };
    }

    public static string Format(double value)
    {
        return value switch
        {
            >= 1_000_000_000 => $"{value / 1_000_000_000:0.#}B",
            >= 1_000_000 => $"{value / 1_000_000:0.#}M",
            >= 1_000 => $"{value / 1_000:0.#}K",
            _ => $"{value:0}",
        };
    }
}
