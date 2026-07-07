namespace TokenOmamoriTool.Services;

/// <summary>
/// Edge-triggered threshold detector for warning toasts (spec §8.6). Update returns true only on
/// a below→above transition for the given key, so a sustained overrun never re-notifies. The very
/// first observation of a key never fires (there is no "below 70%" previous state to transition
/// from); dropping below the threshold re-arms the key. Same boundary-detection idea as
/// CompactBoundaryTracker, but for usage fractions instead of byte offsets.
/// </summary>
public class WarningEdgeDetector
{
    public const double Threshold = 0.7;

    private readonly Dictionary<string, bool> _wasAbove = new();

    public bool Update(string key, double fraction)
    {
        var isAbove = fraction >= Threshold;
        var fired = _wasAbove.TryGetValue(key, out var wasAbove) && !wasAbove && isAbove;
        _wasAbove[key] = isAbove;
        return fired;
    }
}
