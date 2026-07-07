using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class CcusageRunner
{
    public static async Task<CcusageStatus> RunAsync()
    {
        var nodeCheck = await ExternalCommandRunner.RunAsync("node", new[] { "--version" }, TimeSpan.FromSeconds(5));
        if (!nodeCheck.Started)
        {
            return new CcusageStatus { NodeAvailable = false, DisplayText = LocalizationService.T("Ccusage_NodeMissing") };
        }

        // npx is a .cmd wrapper on Windows; CreateProcess won't resolve it without going through cmd.exe.
        var result = await ExternalCommandRunner.RunAsync(
            "cmd.exe",
            new[] { "/c", "npx", "ccusage@latest", "daily", "--json" },
            TimeSpan.FromSeconds(45));

        if (!result.Started || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return new CcusageStatus { NodeAvailable = true, ParsedOk = false, DisplayText = LocalizationService.T("Ccusage_RunFailed") };
        }

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var parsed = CcusageParser.TryParseToday(result.StandardOutput, today);
        if (parsed is not null) return parsed;

        return new CcusageStatus { NodeAvailable = true, ParsedOk = false, DisplayText = LocalizationService.T("Ccusage_ParseFailed") };
    }
}
