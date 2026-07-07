using System.IO;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class RtkUninstaller
{
    public static IReadOnlyList<OperationStep> BuildUninstallSteps()
    {
        var removeHook = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_RemoveHook"),
            CommandPreview = "rtk init -g --uninstall",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync("rtk", new[] { "init", "-g", "--uninstall" }, TimeSpan.FromSeconds(15));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(LocalizationService.T("RtkStep_RemoveHook_Ok"))
                    : OperationStepResult.Fail(LocalizationService.F("RtkStep_RemoveHook_Failed", result.StandardError));
            },
        };

        var removeBinary = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_RemoveBinary"),
            CommandPreview = LocalizationService.T("RtkStep_RemoveBinary_Preview"),
            Run = async () =>
            {
                var method = RtkInstallState.GetMethod();
                if (method == "cargo")
                {
                    var result = await ExternalCommandRunner.RunAsync("cargo", new[] { "uninstall", "rtk" }, TimeSpan.FromSeconds(30));
                    return result.Started && result.ExitCode == 0
                        ? OperationStepResult.Ok(LocalizationService.T("RtkStep_RemoveBinary_CargoOk"))
                        : OperationStepResult.Fail(LocalizationService.F("RtkStep_RemoveBinary_CargoFailed", result.StandardError));
                }

                if (Directory.Exists(RtkInstaller.ToolsDir))
                {
                    Directory.Delete(RtkInstaller.ToolsDir, recursive: true);
                }
                return OperationStepResult.Ok(LocalizationService.T("RtkStep_RemoveBinary_Ok"));
            },
        };

        return new[] { removeHook, removeBinary };
    }
}
