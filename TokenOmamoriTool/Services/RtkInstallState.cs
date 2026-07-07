using System.IO;
using System.Text.Json;

namespace TokenOmamoriTool.Services;

// Tracks whether RTK was installed via the GitHub Releases binary or via `cargo install`, so
// uninstall knows whether to run `cargo uninstall rtk` or just delete the extracted binary.
// Deliberately kept separate from the user-facing settings.json (monitoring config).
public static class RtkInstallState
{
    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TokenOmamoriTool", "tool-install-state.json");

    public static void SetMethod(string method)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(new { rtkInstallMethod = method }));
    }

    public static string? GetMethod()
    {
        if (!File.Exists(StatePath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(StatePath));
            return doc.RootElement.TryGetProperty("rtkInstallMethod", out var v) ? v.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
