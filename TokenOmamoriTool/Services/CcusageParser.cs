using System.Text.Json;
using System.Text.Json.Serialization;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

// Parses the JSON produced by `npx ccusage@latest daily --json` (schema confirmed against a real
// run: a top-level "daily" array of per-day entries, each carrying a "period" date string).
public static class CcusageParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CcusageStatus? TryParseToday(string json, string todayPeriod)
    {
        CcusageDailyReport? report;
        try
        {
            report = JsonSerializer.Deserialize<CcusageDailyReport>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        var todayEntry = report?.Daily?.FirstOrDefault(d => d.Period == todayPeriod);
        if (todayEntry is null)
        {
            return new CcusageStatus
            {
                NodeAvailable = true,
                ParsedOk = true,
                TodayTotalTokens = 0,
                TodayTotalCost = 0,
                DisplayText = LocalizationService.T("Ccusage_Zero"),
            };
        }

        return new CcusageStatus
        {
            NodeAvailable = true,
            ParsedOk = true,
            TodayTotalTokens = todayEntry.TotalTokens,
            TodayTotalCost = todayEntry.TotalCost,
            DisplayText = LocalizationService.F("Ccusage_Display", todayEntry.TotalTokens, todayEntry.TotalCost),
        };
    }

    private class CcusageDailyReport
    {
        [JsonPropertyName("daily")]
        public List<CcusageDailyEntry>? Daily { get; set; }
    }

    private class CcusageDailyEntry
    {
        [JsonPropertyName("period")]
        public string Period { get; set; } = "";

        [JsonPropertyName("totalTokens")]
        public long TotalTokens { get; set; }

        [JsonPropertyName("totalCost")]
        public double TotalCost { get; set; }
    }
}
