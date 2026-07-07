namespace TokenOmamoriTool.Models;

public class RtkGainStatus
{
    public bool Installed { get; set; } = true;
    public bool ParsedOk { get; set; }
    public int? TotalCommands { get; set; }
    public string? TokensSavedText { get; set; }
    public double? SavedPercent { get; set; }
    public string RawOutput { get; set; } = "";
    public string DisplayText { get; set; } = "計測中...";
}
