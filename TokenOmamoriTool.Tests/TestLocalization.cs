using System.Xml.Linq;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

/// <summary>
/// Loads the real Strings.*.xaml dictionaries by parsing them as plain XML (no WPF Application
/// needed) and installs them into LocalizationService. Tests that assert Japanese display strings
/// call UseJapanese() once; the language is process-global, so no test may switch to another
/// language at runtime (they'd race under xUnit's parallel execution).
/// </summary>
internal static class TestLocalization
{
    private static readonly object Lock = new();
    private static bool _initialized;

    public static void UseJapanese()
    {
        lock (Lock)
        {
            if (_initialized) return;
            LocalizationService.ApplyDictionaries("ja", LoadStrings("ja"), LoadStrings("en"));
            _initialized = true;
        }
    }

    public static Dictionary<string, string> LoadStrings(string language) =>
        ParseXamlStrings(GetDictionaryPath(language));

    public static string GetDictionaryPath(string language) =>
        Path.Combine(FindRepoRoot(), "TokenOmamoriTool", "Resources", $"Strings.{language}.xaml");

    public static Dictionary<string, string> ParseXamlStrings(string path)
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var doc = XDocument.Load(path);
        return doc.Root!
            .Elements()
            .Where(e => e.Attribute(x + "Key") is not null)
            .ToDictionary(e => e.Attribute(x + "Key")!.Value, e => e.Value);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
            dir = dir.Parent!;
        }
        throw new InvalidOperationException("Repo root (.sln) not found above " + AppContext.BaseDirectory);
    }
}
