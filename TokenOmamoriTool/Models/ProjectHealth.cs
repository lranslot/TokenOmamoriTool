namespace TokenOmamoriTool.Models;

public class ProjectHealth
{
    public string Name { get; set; } = "";
    public ClaudeMdStatus ClaudeMd { get; set; } = new();
    public SessionLogStatus SessionLog { get; set; } = new();
}

public class ClaudeMdStatus
{
    public bool Exists { get; set; }
    public bool IsOk { get; set; }
    public int LineCount { get; set; }
    public double SizeKB { get; set; }
    public string Message { get; set; } = "";
    public string Icon => !Exists ? "×" : IsOk ? "○" : "△";
    public MetricBar LinesBar { get; set; } = new();
    public MetricBar SizeBar { get; set; } = new();
}

public class SessionLogStatus
{
    public bool Found { get; set; }
    public bool IsOk { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? LastModified { get; set; }
    public string Message { get; set; } = "";
    public string Icon => !Found ? "×" : IsOk ? "○" : "△";
    public MetricBar SizeBar { get; set; } = new();
}

/// <summary>
/// Current-value-vs-limit for one progress bar. Ratio/TrackRatio are clamped to [0,1] so they
/// always sum to 1 (safe for binding straight into proportional Grid columns); UsageFraction is
/// left uncapped so the color threshold still reads correctly when a value exceeds its limit.
/// </summary>
public class MetricBar
{
    public string Label { get; set; } = "";
    public string ValueText { get; set; } = "";
    public double UsageFraction { get; set; }
    public double Ratio => Math.Clamp(UsageFraction, 0.0, 1.0);
    public double TrackRatio => 1.0 - Ratio;

    public static MetricBar Create(string label, double current, double max, string valueText)
    {
        return new MetricBar
        {
            Label = label,
            ValueText = valueText,
            UsageFraction = max > 0 ? current / max : 0,
        };
    }
}
