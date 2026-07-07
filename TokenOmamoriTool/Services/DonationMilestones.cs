namespace TokenOmamoriTool.Services;

/// <summary>
/// Pure milestone logic for the donation banner (spec §9.2) plus the single place the donation
/// page URL lives (spec §9.3). Kept free of any UI/IO so it stays unit-testable and can never
/// interfere with the monitoring features (spec §9.4).
/// </summary>
public static class DonationMilestones
{
    // The single place the donation URL lives (spec §9.3) — README's support section links to
    // the same page.
    public const string DonatePageUrl = "https://ko-fi.com/lranslot";

    public static readonly long[] All = { 100_000, 1_000_000, 10_000_000 };

    /// <summary>
    /// The highest milestone that the cumulative savings have reached and that hasn't been shown
    /// yet, or null. Returning only the highest means a big jump (e.g. first reading is 2M) shows
    /// one banner, not a backlog of three.
    /// </summary>
    public static long? FindUnshownMilestone(double? cumulativeTokensSaved, IReadOnlyCollection<long> shownMilestones)
    {
        if (cumulativeTokensSaved is null) return null;

        var reached = All
            .Where(m => cumulativeTokensSaved.Value >= m && !shownMilestones.Contains(m))
            .ToList();
        return reached.Count == 0 ? null : reached.Max();
    }

    /// <summary>
    /// Closing milestone M also retires every milestone below it, so a lower banner can never pop
    /// up after a higher one was already acknowledged.
    /// </summary>
    public static List<long> MarkShown(IReadOnlyCollection<long> shownMilestones, long milestone)
    {
        return shownMilestones
            .Concat(All.Where(m => m <= milestone))
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }
}
