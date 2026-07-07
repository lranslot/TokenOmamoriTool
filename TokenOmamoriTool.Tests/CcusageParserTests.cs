using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class CcusageParserTests
{
    public CcusageParserTests()
    {
        TestLocalization.UseJapanese(); // DisplayText is built through LocalizationService
    }

    private const string SampleJson = """
    {
      "daily": [
        {
          "agent": "all",
          "period": "2026-07-04",
          "inputTokens": 6031,
          "outputTokens": 336568,
          "totalCost": 30.5,
          "totalTokens": 128830391
        },
        {
          "agent": "all",
          "period": "2026-07-05",
          "inputTokens": 1472,
          "outputTokens": 516391,
          "totalCost": 82.4905456,
          "totalTokens": 359896173
        }
      ],
      "totals": {
        "totalTokens": 488726564,
        "totalCost": 112.9905456
      }
    }
    """;

    [Fact]
    public void TryParseToday_MatchingPeriod_ReturnsTodaysTotals()
    {
        var status = CcusageParser.TryParseToday(SampleJson, "2026-07-05");

        Assert.NotNull(status);
        Assert.True(status!.ParsedOk);
        Assert.Equal(359896173, status.TodayTotalTokens);
        Assert.Equal(82.4905456, status.TodayTotalCost);
        Assert.Contains("359,896,173", status.DisplayText);
    }

    [Fact]
    public void TryParseToday_NoEntryForToday_ReturnsZeroUsage()
    {
        var status = CcusageParser.TryParseToday(SampleJson, "2099-01-01");

        Assert.NotNull(status);
        Assert.True(status!.ParsedOk);
        Assert.Equal(0, status.TodayTotalTokens);
    }

    [Fact]
    public void TryParseToday_InvalidJson_ReturnsNull()
    {
        var status = CcusageParser.TryParseToday("not json at all", "2026-07-05");

        Assert.Null(status);
    }
}
