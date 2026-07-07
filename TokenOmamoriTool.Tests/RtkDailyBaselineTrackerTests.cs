using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class RtkDailyBaselineTrackerTests
{
    [Fact]
    public void ResolveBaseline_NoStoredState_StartsFromCurrentCumulative()
    {
        var today = new DateTime(2026, 7, 6);

        var resolved = RtkDailyBaselineTracker.ResolveBaseline(null, currentCumulative: 456_000, today);

        Assert.Equal("2026-07-06", resolved.Date);
        Assert.Equal(456_000, resolved.BaselineTokensSaved);
    }

    [Fact]
    public void ResolveBaseline_StoredStateIsToday_KeepsExistingBaseline()
    {
        var today = new DateTime(2026, 7, 6);
        var stored = new RtkDailyBaselineState { Date = "2026-07-06", BaselineTokensSaved = 300_000 };

        var resolved = RtkDailyBaselineTracker.ResolveBaseline(stored, currentCumulative: 456_000, today);

        Assert.Same(stored, resolved);
    }

    [Fact]
    public void ResolveBaseline_StoredStateIsStale_RollsOverToCurrentCumulative()
    {
        var today = new DateTime(2026, 7, 6);
        var stored = new RtkDailyBaselineState { Date = "2026-07-05", BaselineTokensSaved = 300_000 };

        var resolved = RtkDailyBaselineTracker.ResolveBaseline(stored, currentCumulative: 456_000, today);

        Assert.Equal("2026-07-06", resolved.Date);
        Assert.Equal(456_000, resolved.BaselineTokensSaved);
    }
}
