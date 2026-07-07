using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class DonationMilestonesTests
{
    private static readonly long[] None = Array.Empty<long>();

    [Fact]
    public void FindUnshownMilestone_BelowFirstMilestone_ReturnsNull()
    {
        Assert.Null(DonationMilestones.FindUnshownMilestone(99_999, None));
    }

    [Fact]
    public void FindUnshownMilestone_UnknownCumulative_ReturnsNull()
    {
        Assert.Null(DonationMilestones.FindUnshownMilestone(null, None));
    }

    [Fact]
    public void FindUnshownMilestone_ExactlyAtMilestone_ReturnsIt()
    {
        Assert.Equal(100_000, DonationMilestones.FindUnshownMilestone(100_000, None));
    }

    [Fact]
    public void FindUnshownMilestone_JumpPastSeveral_ReturnsOnlyTheHighest()
    {
        // First reading is already 2M: show a single 1M banner, not a backlog of two.
        Assert.Equal(1_000_000, DonationMilestones.FindUnshownMilestone(2_000_000, None));
    }

    [Fact]
    public void FindUnshownMilestone_AlreadyShown_IsExcluded()
    {
        var shown = new long[] { 100_000, 1_000_000 };
        Assert.Null(DonationMilestones.FindUnshownMilestone(2_000_000, shown));
    }

    [Fact]
    public void FindUnshownMilestone_NextMilestoneAfterShownOnes_Fires()
    {
        var shown = new long[] { 100_000, 1_000_000 };
        Assert.Equal(10_000_000, DonationMilestones.FindUnshownMilestone(10_000_000, shown));
    }

    [Fact]
    public void MarkShown_RetiresTheMilestoneAndEverythingBelow()
    {
        var result = DonationMilestones.MarkShown(Array.Empty<long>(), 1_000_000);
        Assert.Equal(new long[] { 100_000, 1_000_000 }, result);
    }

    [Fact]
    public void MarkShown_IsIdempotent()
    {
        var once = DonationMilestones.MarkShown(Array.Empty<long>(), 100_000);
        var twice = DonationMilestones.MarkShown(once, 100_000);
        Assert.Equal(once, twice);
    }

    [Fact]
    public void MarkShown_KeepsExistingEntries()
    {
        var result = DonationMilestones.MarkShown(new long[] { 10_000_000 }, 100_000);
        Assert.Equal(new long[] { 100_000, 10_000_000 }, result);
    }
}
