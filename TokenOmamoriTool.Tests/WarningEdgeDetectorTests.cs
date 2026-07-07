using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class WarningEdgeDetectorTests
{
    [Fact]
    public void FirstObservationBelowThreshold_DoesNotFire()
    {
        var detector = new WarningEdgeDetector();
        Assert.False(detector.Update("claudeMd", 0.5));
    }

    [Fact]
    public void FirstObservationAboveThreshold_DoesNotFire()
    {
        // Strict edge trigger: 70%未満→70%以上 の遷移のみ発火。初回観測は「70%未満だった」
        // 状態が存在しないので発火しない(起動のたびに通知しない)。
        var detector = new WarningEdgeDetector();
        Assert.False(detector.Update("claudeMd", 0.9));
    }

    [Fact]
    public void RisingAcrossThreshold_Fires()
    {
        var detector = new WarningEdgeDetector();
        detector.Update("claudeMd", 0.5);
        Assert.True(detector.Update("claudeMd", 0.75));
    }

    [Fact]
    public void ExactlyAtThreshold_CountsAsAbove()
    {
        var detector = new WarningEdgeDetector();
        detector.Update("claudeMd", 0.69);
        Assert.True(detector.Update("claudeMd", 0.7));
    }

    [Fact]
    public void SustainedOverrun_DoesNotRefire()
    {
        var detector = new WarningEdgeDetector();
        detector.Update("claudeMd", 0.5);
        Assert.True(detector.Update("claudeMd", 0.75));
        Assert.False(detector.Update("claudeMd", 0.8));
        Assert.False(detector.Update("claudeMd", 0.95));
    }

    [Fact]
    public void DroppingBelowThenRisingAgain_RefiresOnce()
    {
        var detector = new WarningEdgeDetector();
        detector.Update("claudeMd", 0.5);
        Assert.True(detector.Update("claudeMd", 0.75));
        Assert.False(detector.Update("claudeMd", 0.6));
        Assert.True(detector.Update("claudeMd", 0.75));
        Assert.False(detector.Update("claudeMd", 0.75));
    }

    [Fact]
    public void KeysAreIndependent()
    {
        var detector = new WarningEdgeDetector();
        detector.Update("claudeMd", 0.5);
        detector.Update("sessionLog", 0.5);

        Assert.True(detector.Update("claudeMd", 0.75));
        // claudeMd firing must not consume or disturb sessionLog's state.
        Assert.True(detector.Update("sessionLog", 0.8));
        Assert.False(detector.Update("claudeMd", 0.8));
        Assert.False(detector.Update("sessionLog", 0.9));
    }
}
