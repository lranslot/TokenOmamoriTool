namespace TokenOmamoriTool.Services;

public enum ClaudeMemInstallState
{
    Installed,
    NotInstalled,
    CliMissing,
}

public static class ClaudeMemStatusChecker
{
    public static async Task<ClaudeMemInstallState> CheckAsync()
    {
        // `claude` has no .exe form on Windows (only .cmd), so a direct Process.Start("claude", ...)
        // always fails with Win32Exception regardless of whether it's actually installed (same
        // PATHEXT gotcha as npx — see CcusageRunner). Always go through cmd.exe, and use the exit
        // code (not Started) to detect "claude isn't usable".
        var result = await ExternalCommandRunner.RunAsync(
            "cmd.exe", new[] { "/c", "claude", "plugin", "list" }, TimeSpan.FromSeconds(10));

        if (!result.Started || result.ExitCode != 0) return ClaudeMemInstallState.CliMissing;

        return result.StandardOutput.Contains("claude-mem", StringComparison.OrdinalIgnoreCase)
            ? ClaudeMemInstallState.Installed
            : ClaudeMemInstallState.NotInstalled;
    }
}
