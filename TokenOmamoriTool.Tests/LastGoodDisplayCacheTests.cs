using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class LastGoodDisplayCacheTests
{
    public LastGoodDisplayCacheTests()
    {
        TestLocalization.UseJapanese(); // the stale suffix is built through LocalizationService
    }

    private static readonly DateOnly Day1 = new(2026, 7, 18);
    private static readonly DateOnly Day2 = new(2026, 7, 19);

    [Fact]
    public void Success_ReturnsFreshText()
    {
        var cache = new LastGoodDisplayCache();

        Assert.Equal("本日消費：12,300 tokens ($1.23)",
            cache.Resolve(true, "本日消費：12,300 tokens ($1.23)", Day1));
    }

    [Fact]
    public void FailureAfterSuccess_SameDay_KeepsLastGoodWithStaleMark()
    {
        var cache = new LastGoodDisplayCache();
        cache.Resolve(true, "本日消費：12,300 tokens ($1.23)", Day1);

        var text = cache.Resolve(false, "本日消費：実行に失敗しました", Day1);

        Assert.Equal("本日消費：12,300 tokens ($1.23)（更新失敗）", text);
    }

    [Fact]
    public void FailureWithoutAnySuccess_ReturnsFailureText()
    {
        var cache = new LastGoodDisplayCache();

        Assert.Equal("本日消費：実行に失敗しました",
            cache.Resolve(false, "本日消費：実行に失敗しました", Day1));
    }

    [Fact]
    public void FailureAfterDateRollover_DiscardsPreviousDayValue()
    {
        var cache = new LastGoodDisplayCache();
        cache.Resolve(true, "本日消費：12,300 tokens ($1.23)", Day1);

        var text = cache.Resolve(false, "本日消費：実行に失敗しました", Day2);

        Assert.Equal("本日消費：実行に失敗しました", text);
        // The discarded value must not resurface on a later same-day failure either.
        Assert.Equal("本日消費：実行に失敗しました",
            cache.Resolve(false, "本日消費：実行に失敗しました", Day2));
    }

    [Fact]
    public void SuccessAfterFailure_ReturnsFreshTextAndUpdatesLastGood()
    {
        var cache = new LastGoodDisplayCache();
        cache.Resolve(true, "本日消費：100 tokens ($0.01)", Day1);
        cache.Resolve(false, "本日消費：実行に失敗しました", Day1);

        Assert.Equal("本日消費：200 tokens ($0.02)",
            cache.Resolve(true, "本日消費：200 tokens ($0.02)", Day1));
        Assert.Equal("本日消費：200 tokens ($0.02)（更新失敗）",
            cache.Resolve(false, "本日消費：実行に失敗しました", Day1));
    }
}
