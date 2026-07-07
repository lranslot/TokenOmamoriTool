namespace TokenOmamoriTool.Models;

public class AppSettings
{
    public string TargetAi { get; set; } = "claude";
    // "ja" / "en". Null (missing key) means first run: resolved from the OS UI culture and then
    // persisted (spec v0.3追補 §11.4).
    public string? Language { get; set; }
    public InstructionFileSettings InstructionFile { get; set; } = new();
    public SessionLogSettings SessionLog { get; set; } = new();
    public ExternalToolsSettings ExternalTools { get; set; } = new();
    public int PollIntervalSeconds { get; set; } = 5;
    public TraySettings Tray { get; set; } = new();
    public DonationSettings Donation { get; set; } = new();
    public List<MonitoredProject> Projects { get; set; } = new();
}

public class DonationSettings
{
    // Milestones whose banner was already shown and closed (spec §9.2) — never shown again.
    public List<long> ShownMilestones { get; set; } = new();
}

public class TraySettings
{
    public bool HideOnClose { get; set; } = true;
    public bool ToastOnWarning { get; set; } = true;
    // Internal flag (not shown in the settings window): whether the one-time
    // "resident in tray" notice has already been shown.
    public bool FirstHideNoticeShown { get; set; }
}

public class ExternalToolsSettings
{
    public int PollIntervalSeconds { get; set; } = 60;
}

public class InstructionFileSettings
{
    public string Path { get; set; } = "CLAUDE.md";
    public int MaxLines { get; set; } = 200;
    public int MaxSizeKB { get; set; } = 25;
}

public class SessionLogSettings
{
    public string? ConfigDirOverride { get; set; }
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
}

public class MonitoredProject
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}
