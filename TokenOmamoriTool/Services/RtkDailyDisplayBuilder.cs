using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class RtkDailyDisplayBuilder
{
    // Returns null when the cumulative figure can't be parsed back into a number (e.g. rtk gain's
    // output format changed) so the caller can fall back to the raw RtkGainStatus.DisplayText.
    public static string? BuildDisplayText(RtkGainStatus status, RtkDailyBaselineState baseline)
    {
        var todayText = BuildTodayText(status, baseline);
        if (todayText is null) return null;

        return LocalizationService.F("Rtk_DailyDisplay", todayText, status.TokensSavedText);
    }

    // Just today's savings (e.g. "12.3K"), for the tray tooltip's「本日節約」item. Null when the
    // cumulative figure can't be parsed, so the tooltip omits the item entirely (spec §8.4).
    public static string? BuildTodayText(RtkGainStatus status, RtkDailyBaselineState baseline)
    {
        if (!status.ParsedOk || status.TokensSavedText is null) return null;

        var cumulative = TokenCountFormatter.TryParse(status.TokensSavedText);
        if (cumulative is null) return null;

        var today = Math.Max(0, cumulative.Value - baseline.BaselineTokensSaved);
        return TokenCountFormatter.Format(today);
    }
}
