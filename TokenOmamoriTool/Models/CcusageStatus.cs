namespace TokenOmamoriTool.Models;

public class CcusageStatus
{
    public bool NodeAvailable { get; set; } = true;
    public bool ParsedOk { get; set; }
    public long? TodayTotalTokens { get; set; }
    public double? TodayTotalCost { get; set; }
    public string DisplayText { get; set; } = "計測中...";
}
