using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class ClaudeMdMonitorTests : IDisposable
{
    private readonly string _tempDir;

    public ClaudeMdMonitorTests()
    {
        TestLocalization.UseJapanese(); // assertions below check Japanese display strings
        _tempDir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "TokenOmamoriTool.Tests." + Guid.NewGuid())).FullName;
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private static InstructionFileSettings DefaultSettings() => new()
    {
        Path = "CLAUDE.md",
        MaxLines = 200,
        MaxSizeKB = 25,
    };

    [Fact]
    public void Check_FileMissing_ReturnsNotExists()
    {
        var status = ClaudeMdMonitor.Check(_tempDir, DefaultSettings());

        Assert.False(status.Exists);
        Assert.False(status.IsOk);
    }

    [Fact]
    public void Check_WithinThresholds_ReturnsOk()
    {
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), "line1\nline2\nline3");

        var status = ClaudeMdMonitor.Check(_tempDir, DefaultSettings());

        Assert.True(status.Exists);
        Assert.True(status.IsOk);
        Assert.Equal(3, status.LineCount);
        Assert.Equal(3 / 200.0, status.LinesBar.UsageFraction, precision: 5);
        Assert.Equal("3/200行", status.LinesBar.ValueText);
    }

    [Fact]
    public void Check_LinesBarRatio_IsClampedForOverflowButUsageFractionIsNot()
    {
        var content = string.Join("\n", Enumerable.Repeat("line", 400)); // 2x the 200-line limit
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), content);

        var status = ClaudeMdMonitor.Check(_tempDir, DefaultSettings());

        Assert.Equal(2.0, status.LinesBar.UsageFraction, precision: 5);
        Assert.Equal(1.0, status.LinesBar.Ratio);
        Assert.Equal(0.0, status.LinesBar.TrackRatio);
    }

    [Fact]
    public void Check_ExceedsMaxLines_ReturnsWarning()
    {
        var content = string.Join("\n", Enumerable.Repeat("line", 201));
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), content);

        var status = ClaudeMdMonitor.Check(_tempDir, DefaultSettings());

        Assert.True(status.Exists);
        Assert.False(status.IsOk);
        Assert.Contains("肥大化", status.Message);
    }

    [Fact]
    public void Check_ExceedsMaxSizeKB_ReturnsWarning()
    {
        var settings = DefaultSettings();
        settings.MaxLines = 10_000; // isolate the size threshold from the line threshold
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), new string('a', 30 * 1024));

        var status = ClaudeMdMonitor.Check(_tempDir, settings);

        Assert.True(status.Exists);
        Assert.False(status.IsOk);
    }
}
