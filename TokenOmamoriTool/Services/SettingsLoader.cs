using System.IO;
using System.Text.Json;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class SettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static AppSettings Load(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            var defaultSettings = CreateDefault();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(defaultSettings, JsonOptions));
            return defaultSettings;
        }

        var json = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefault();
    }

    public static void Save(string settingsPath, AppSettings settings)
    {
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static AppSettings CreateDefault()
    {
        // Find the repo root (the directory containing TokenOmamoriTool.sln) so a first run
        // has a real, non-empty project to display instead of an empty list.
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;

        var settings = new AppSettings();
        settings.Projects.Add(new MonitoredProject
        {
            Name = Path.GetFileName(repoRoot.TrimEnd(Path.DirectorySeparatorChar)),
            Path = repoRoot,
        });
        return settings;
    }

    private static string? FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
