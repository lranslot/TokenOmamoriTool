using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class ClaudeMemInstaller
{
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude-mem", "settings.json");

    public static IReadOnlyList<OperationStep> BuildInstallSteps()
    {
        var addMarketplace = new OperationStep
        {
            Description = LocalizationService.T("CmStep_AddMarketplace"),
            CommandPreview = "claude plugin marketplace add thedotmack/claude-mem",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync(
                    "cmd.exe", new[] { "/c", "claude", "plugin", "marketplace", "add", "thedotmack/claude-mem" }, TimeSpan.FromSeconds(30));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(result.StandardOutput.Trim())
                    : OperationStepResult.Fail(LocalizationService.F("CmStep_AddMarketplace_Failed", result.StandardError));
            },
        };

        var installPlugin = new OperationStep
        {
            Description = LocalizationService.T("CmStep_Install"),
            CommandPreview = "claude plugin install claude-mem",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync(
                    "cmd.exe", new[] { "/c", "claude", "plugin", "install", "claude-mem" }, TimeSpan.FromSeconds(60));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(result.StandardOutput.Trim())
                    : OperationStepResult.Fail(LocalizationService.F("CmStep_Install_Failed", result.StandardError));
            },
        };

        var applySettings = new OperationStep
        {
            Description = LocalizationService.T("CmStep_ApplySettings"),
            CommandPreview = LocalizationService.F("CmStep_ApplySettings_Preview", SettingsPath),
            Run = async () =>
            {
                try
                {
                    await Task.Run(ApplyLowTokenSettings);
                    return OperationStepResult.Ok(LocalizationService.T("CmStep_ApplySettings_Ok"));
                }
                catch (Exception ex) when (ex is IOException or JsonException)
                {
                    return OperationStepResult.Fail(LocalizationService.F("CmStep_ApplySettings_Failed", ex.Message));
                }
            },
        };

        return new[] { addMarketplace, installPlugin, applySettings };
    }

    private static void ApplyLowTokenSettings()
    {
        JsonNode root;
        if (File.Exists(SettingsPath))
        {
            var backupPath = SettingsPath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss");
            File.Copy(SettingsPath, backupPath, overwrite: false);
            root = JsonNode.Parse(File.ReadAllText(SettingsPath)) ?? new JsonObject();
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            root = new JsonObject();
        }

        root["CONTEXT_OBSERVATIONS"] = 1;
        root["CONTEXT_FULL_COUNT"] = 0;
        root["CONTEXT_SESSION_COUNT"] = 1;

        File.WriteAllText(SettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
