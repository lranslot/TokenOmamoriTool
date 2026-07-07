using System.Text;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class CompactBoundaryTrackerTests : IDisposable
{
    private readonly string _filePath;

    public CompactBoundaryTrackerTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "CompactBoundaryTrackerTests." + Guid.NewGuid() + ".jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }

    [Fact]
    public void GetEffectiveSizeBytes_NoCompactMarker_ReturnsFullFileLength()
    {
        File.WriteAllText(_filePath, "{\"type\":\"user\"}\n{\"type\":\"assistant\"}\n");
        var length = new FileInfo(_filePath).Length;

        var effective = CompactBoundaryTracker.GetEffectiveSizeBytes(_filePath, length);

        Assert.Equal(length, effective);
    }

    [Fact]
    public void GetEffectiveSizeBytes_WithCompactMarker_OnlyCountsBytesAfterIt()
    {
        var before = "{\"type\":\"user\",\"content\":\"" + new string('a', 5000) + "\"}\n";
        var compactLine = "{\"type\":\"user\",\"isCompactSummary\":true,\"uuid\":\"x\"}\n";
        var after = "{\"type\":\"assistant\",\"content\":\"short\"}\n";
        File.WriteAllText(_filePath, before + compactLine + after);
        var length = new FileInfo(_filePath).Length;

        var effective = CompactBoundaryTracker.GetEffectiveSizeBytes(_filePath, length);

        Assert.Equal(Encoding.UTF8.GetByteCount(after), effective);
    }

    [Fact]
    public void GetEffectiveSizeBytes_IncrementalAppend_DetectsNewCompactAcrossMultipleBackwardChunks()
    {
        // First check: a small line, then a large still-unterminated "line" (> 64KB backward-search chunk size)
        // with no compact marker yet.
        var firstLine = "{\"type\":\"user\",\"content\":\"first\"}\n";
        var bigUnterminatedChunk = new string('x', 70_000);
        File.WriteAllText(_filePath, firstLine + bigUnterminatedChunk);
        var firstLength = new FileInfo(_filePath).Length;

        var firstEffective = CompactBoundaryTracker.GetEffectiveSizeBytes(_filePath, firstLength);
        Assert.Equal(firstLength, firstEffective);

        // Second check: the big line finally terminates, then a compact marker, then a short tail.
        var compactLine = "{\"type\":\"user\",\"isCompactSummary\":true,\"uuid\":\"y\"}\n";
        var tail = "{\"type\":\"assistant\",\"content\":\"ok\"}\n";
        File.AppendAllText(_filePath, "\n" + compactLine + tail);
        var secondLength = new FileInfo(_filePath).Length;

        var secondEffective = CompactBoundaryTracker.GetEffectiveSizeBytes(_filePath, secondLength);

        Assert.Equal(Encoding.UTF8.GetByteCount(tail), secondEffective);
    }
}
