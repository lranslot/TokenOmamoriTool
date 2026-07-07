using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class RtkDailyDisplayBuilderTests
{
    public RtkDailyDisplayBuilderTests()
    {
        TestLocalization.UseJapanese(); // assertions below check Japanese display strings
    }

    [Fact]
    public void BuildDisplayText_ComputesTodayDeltaAgainstBaseline()
    {
        var status = new RtkGainStatus { Installed = true, ParsedOk = true, TokensSavedText = "456K" };
        var baseline = new RtkDailyBaselineState { Date = "2026-07-06", BaselineTokensSaved = 300_000 };

        var text = RtkDailyDisplayBuilder.BuildDisplayText(status, baseline);

        Assert.Equal("節約(RTK)：本日 156K / 累計 456K tokens", text);
    }

    [Fact]
    public void BuildDisplayText_TodayDeltaNeverNegative()
    {
        // Baseline can exceed the current cumulative if RTK's own counter was ever reset externally;
        // the daily figure must clamp at 0 rather than show a negative number.
        var status = new RtkGainStatus { Installed = true, ParsedOk = true, TokensSavedText = "100K" };
        var baseline = new RtkDailyBaselineState { Date = "2026-07-06", BaselineTokensSaved = 300_000 };

        var text = RtkDailyDisplayBuilder.BuildDisplayText(status, baseline);

        Assert.Equal("節約(RTK)：本日 0 / 累計 100K tokens", text);
    }

    [Fact]
    public void BuildDisplayText_ParseFailure_ReturnsNull()
    {
        var status = new RtkGainStatus { Installed = true, ParsedOk = false, TokensSavedText = null };
        var baseline = new RtkDailyBaselineState { Date = "2026-07-06", BaselineTokensSaved = 0 };

        Assert.Null(RtkDailyDisplayBuilder.BuildDisplayText(status, baseline));
    }
}
