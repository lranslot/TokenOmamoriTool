using System.Reflection;

namespace TokenOmamoriTool.Services;

/// <summary>
/// Version display for the help window (spec §13.3): always derived from the assembly metadata
/// (which the csproj &lt;Version&gt; generates) — never hardcoded. The formatting is split out so
/// it can be unit-tested without WPF (§13.7).
/// </summary>
public static class AppVersionInfo
{
    /// <summary>Formats as "X.Y.Z" (assembly versions carry a 4th revision part — dropped).</summary>
    public static string DisplayVersion(Version? version) =>
        version is null ? "?" : $"{version.Major}.{version.Minor}.{version.Build}";

    public static string CurrentDisplayVersion() =>
        DisplayVersion(typeof(AppVersionInfo).Assembly.GetName().Version);
}
