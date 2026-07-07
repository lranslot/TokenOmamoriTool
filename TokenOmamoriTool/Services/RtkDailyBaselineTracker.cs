using System.IO;
using System.Text.Json;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

// Tracks the RTK cumulative-savings value as of the start of "today" so the UI can show a same-day
// delta alongside the lifetime total, without ever touching RTK's own data. Deliberately kept
// separate from the user-facing settings.json (internal derived state, like RtkInstallState).
public static class RtkDailyBaselineTracker
{
    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TokenOmamoriTool", "rtk-daily-baseline.json");

    // Pure: if the stored baseline is still for `today`, keep it; otherwise roll over to a fresh
    // baseline pinned at the current cumulative value. Called on every poll (60s), at startup, and
    // from the midnight one-shot timer, so a missed/late trigger is always caught by the next one.
    public static RtkDailyBaselineState ResolveBaseline(RtkDailyBaselineState? stored, double currentCumulative, DateTime today)
    {
        var todayKey = today.ToString("yyyy-MM-dd");
        if (stored is not null && stored.Date == todayKey) return stored;

        return new RtkDailyBaselineState { Date = todayKey, BaselineTokensSaved = currentCumulative };
    }

    public static RtkDailyBaselineState? Load()
    {
        if (!File.Exists(StatePath)) return null;
        try
        {
            return JsonSerializer.Deserialize<RtkDailyBaselineState>(File.ReadAllText(StatePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void Save(RtkDailyBaselineState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
    }
}
