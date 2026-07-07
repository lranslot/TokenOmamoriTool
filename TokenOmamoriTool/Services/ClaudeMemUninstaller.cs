using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class ClaudeMemUninstaller
{
    public static IReadOnlyList<OperationStep> BuildUninstallSteps()
    {
        var uninstallPlugin = new OperationStep
        {
            Description = LocalizationService.T("CmStep_Uninstall"),
            CommandPreview = "claude plugin uninstall claude-mem",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync(
                    "cmd.exe", new[] { "/c", "claude", "plugin", "uninstall", "claude-mem" }, TimeSpan.FromSeconds(30));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(result.StandardOutput.Trim())
                    : OperationStepResult.Fail(LocalizationService.F("CmStep_Uninstall_Failed", result.StandardError));
            },
        };

        return new[] { uninstallPlugin };
    }
}
