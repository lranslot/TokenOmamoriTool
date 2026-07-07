using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class SessionLogMonitorTests : IDisposable
{
    private readonly string _configDir;
    private const string ProjectPath = @"c:\fake\project";

    public SessionLogMonitorTests()
    {
        TestLocalization.UseJapanese(); // assertions below check Japanese display strings
        _configDir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "TokenOmamoriTool.Tests.Config." + Guid.NewGuid())).FullName;
    }

    public void Dispose() => Directory.Delete(_configDir, recursive: true);

    private string ProjectSessionDir()
    {
        var encoded = ProjectPathEncoder.Encode(ProjectPath);
        return Directory.CreateDirectory(Path.Combine(_configDir, "projects", encoded)).FullName;
    }

    private SessionLogSettings Settings(long maxSizeBytes = 5 * 1024 * 1024) => new()
    {
        ConfigDirOverride = _configDir,
        MaxSizeBytes = maxSizeBytes,
    };

    [Fact]
    public void Check_NoSessionDirectory_ReturnsNotFoundButOk()
    {
        var status = SessionLogMonitor.Check(ProjectPath, Settings());

        Assert.False(status.Found);
        Assert.True(status.IsOk);
    }

    [Fact]
    public void Check_ExcludesAgentPrefixedFiles_AndPicksMostRecent()
    {
        var dir = ProjectSessionDir();
        var older = Path.Combine(dir, "session-old.jsonl");
        var newer = Path.Combine(dir, "session-new.jsonl");
        var agent = Path.Combine(dir, "agent-sub.jsonl");

        File.WriteAllText(older, "old");
        File.WriteAllText(agent, new string('x', 10_000_000)); // huge, must be ignored
        Thread.Sleep(50);
        File.WriteAllText(newer, "newest content");
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-5));

        var status = SessionLogMonitor.Check(ProjectPath, Settings());

        Assert.True(status.Found);
        Assert.True(status.IsOk);
        Assert.Equal(new FileInfo(newer).Length, status.SizeBytes);
    }

    [Fact]
    public void Check_LatestFileExceedsThreshold_ReturnsWarning()
    {
        var dir = ProjectSessionDir();
        File.WriteAllText(Path.Combine(dir, "session.jsonl"), new string('a', 2000));

        var status = SessionLogMonitor.Check(ProjectPath, Settings(maxSizeBytes: 1000));

        Assert.True(status.Found);
        Assert.False(status.IsOk);
        Assert.Contains("/compact", status.Message);
    }

    [Fact]
    public void Check_FileExceedsThresholdButHasCompactMarker_MeasuresOnlySinceCompact()
    {
        var dir = ProjectSessionDir();
        var before = "{\"type\":\"user\",\"content\":\"" + new string('a', 2000) + "\"}\n";
        var compactLine = "{\"type\":\"user\",\"isCompactSummary\":true}\n";
        var after = "{\"type\":\"assistant\",\"content\":\"ok\"}\n";
        File.WriteAllText(Path.Combine(dir, "session.jsonl"), before + compactLine + after);

        var status = SessionLogMonitor.Check(ProjectPath, Settings(maxSizeBytes: 1000));

        Assert.True(status.Found);
        Assert.True(status.IsOk);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(after), status.SizeBytes);
    }
}
