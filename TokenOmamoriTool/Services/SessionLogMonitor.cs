using System.IO;
using System.Linq;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class SessionLogMonitor
{
    public static SessionLogStatus Check(string projectPath, SessionLogSettings settings)
    {
        var configDir = settings.ConfigDirOverride
            ?? Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude");

        var encoded = ProjectPathEncoder.Encode(projectPath);
        var sessionDir = Path.Combine(configDir, "projects", encoded);

        if (!Directory.Exists(sessionDir))
        {
            return new SessionLogStatus
            {
                Found = false,
                IsOk = true,
                Message = LocalizationService.T("Monitor_SessionNotFound"),
            };
        }

        var latest = new DirectoryInfo(sessionDir)
            .GetFiles("*.jsonl")
            .Where(f => !f.Name.StartsWith("agent-"))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            return new SessionLogStatus
            {
                Found = false,
                IsOk = true,
                Message = LocalizationService.T("Monitor_SessionNotFound"),
            };
        }

        var effectiveSizeBytes = CompactBoundaryTracker.GetEffectiveSizeBytes(latest.FullName, latest.Length);
        var sinceCompact = effectiveSizeBytes < latest.Length;
        var isOk = effectiveSizeBytes <= settings.MaxSizeBytes;
        var sizeKB = effectiveSizeBytes / 1024.0;
        var maxKB = settings.MaxSizeBytes / 1024.0;

        return new SessionLogStatus
        {
            Found = true,
            IsOk = isOk,
            SizeBytes = effectiveSizeBytes,
            LastModified = latest.LastWriteTime,
            Message = isOk
                ? LocalizationService.F("Monitor_SessionOk", sizeKB)
                : LocalizationService.F(
                    sinceCompact ? "Monitor_SessionBloatedSinceCompact" : "Monitor_SessionBloated",
                    sizeKB / 1024.0),
            SizeBar = MetricBar.Create(
                LocalizationService.T("Bar_SessionLog"), sizeKB, maxKB,
                LocalizationService.F("Bar_SessionLogValue", sizeKB, maxKB)),
        };
    }
}
