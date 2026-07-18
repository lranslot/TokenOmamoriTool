namespace TokenOmamoriTool.Services;

// Keeps the last successful display line of an external tool (RTK / ccusage) so a transient
// run/parse failure doesn't replace the shown value with a bare failure message. One instance
// per display line. In-memory only — a restart starts empty, and a value recorded on a previous
// day is discarded so yesterday's total is never shown as today's.
public sealed class LastGoodDisplayCache
{
    private string? _lastGoodText;
    private DateOnly _lastGoodDate;

    // On success: records freshText as the new last-good value and returns it.
    // On failure: returns the same-day last-good value marked as stale, or freshText (the
    // caller's failure message) if there is none.
    public string Resolve(bool success, string freshText, DateOnly today)
    {
        if (success)
        {
            _lastGoodText = freshText;
            _lastGoodDate = today;
            return freshText;
        }

        if (_lastGoodText is not null && _lastGoodDate != today)
        {
            _lastGoodText = null;
        }

        return _lastGoodText is null
            ? freshText
            : LocalizationService.F("External_LastKnownStale", _lastGoodText);
    }
}
