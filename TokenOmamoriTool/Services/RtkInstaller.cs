using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class RtkInstaller
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/rtk-ai/rtk/releases/latest";
    private const string RustupInitUrl = "https://win.rustup.rs/x86_64";

    public static string ToolsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TokenOmamoriTool", "tools", "rtk");

    public static IReadOnlyList<OperationStep> BuildInstallSteps()
    {
        string? downloadUrl = null;
        string? exePath = null;

        var fetchRelease = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_FetchRelease"),
            CommandPreview = $"GET {ReleasesApiUrl}",
            Run = async () =>
            {
                try
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenOmamoriTool");
                    var json = await http.GetStringAsync(ReleasesApiUrl);
                    using var doc = JsonDocument.Parse(json);
                    foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.Contains("pc-windows-msvc", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip"))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            return OperationStepResult.Ok(LocalizationService.F("RtkStep_FetchRelease_Found", name, downloadUrl));
                        }
                    }
                    return OperationStepResult.Fail(LocalizationService.T("RtkStep_FetchRelease_NoAsset"));
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException)
                {
                    return OperationStepResult.Fail(LocalizationService.F("RtkStep_FetchRelease_Failed", ex.Message));
                }
            },
        };

        var downloadAndExtract = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_Download"),
            CommandPreview = LocalizationService.T("RtkStep_Download_Preview"),
            Run = async () =>
            {
                if (downloadUrl is null)
                    return OperationStepResult.Fail(LocalizationService.T("RtkStep_Download_NoUrl"));

                var zipPath = Path.Combine(Path.GetTempPath(), "rtk-download-" + Guid.NewGuid() + ".zip");
                try
                {
                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenOmamoriTool");
                        var bytes = await http.GetByteArrayAsync(downloadUrl);
                        await File.WriteAllBytesAsync(zipPath, bytes);
                    }

                    if (Directory.Exists(ToolsDir)) Directory.Delete(ToolsDir, recursive: true);
                    Directory.CreateDirectory(ToolsDir);
                    ZipFile.ExtractToDirectory(zipPath, ToolsDir);

                    exePath = Directory.GetFiles(ToolsDir, "rtk.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (exePath is null)
                        return OperationStepResult.Fail(LocalizationService.T("RtkStep_Download_NoExe"));

                    AddToPath(Path.GetDirectoryName(exePath)!);
                    RtkInstallState.SetMethod("binary");
                    return OperationStepResult.Ok(LocalizationService.F("RtkStep_Download_Placed", exePath));
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException)
                {
                    return OperationStepResult.Fail(LocalizationService.F("RtkStep_Download_Failed", ex.Message));
                }
                finally
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                }
            },
        };

        var cargoFallback = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_Cargo"),
            CommandPreview = "cargo install --git https://github.com/rtk-ai/rtk",
            Run = async () =>
            {
                if (exePath is not null)
                    return OperationStepResult.Ok(LocalizationService.T("RtkStep_Cargo_SkippedBinaryOk"));

                var cargoCheck = await ExternalCommandRunner.RunAsync("cargo", new[] { "--version" }, TimeSpan.FromSeconds(10));
                if (!cargoCheck.Started)
                {
                    var rustupResult = await InstallRustupAsync();
                    if (!rustupResult.Success) return rustupResult;
                }

                var result = await ExternalCommandRunner.RunAsync(
                    "cargo", new[] { "install", "--git", "https://github.com/rtk-ai/rtk" }, TimeSpan.FromMinutes(5));
                if (!result.Started || result.ExitCode != 0)
                    return OperationStepResult.Fail(LocalizationService.F("RtkStep_Cargo_Failed", result.StandardError));

                RtkInstallState.SetMethod("cargo");
                return OperationStepResult.Ok(LocalizationService.T("RtkStep_Cargo_Ok"));
            },
        };

        var verifyVersion = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_VerifyVersion"),
            CommandPreview = "rtk --version",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync("rtk", new[] { "--version" }, TimeSpan.FromSeconds(10));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(result.StandardOutput.Trim())
                    : OperationStepResult.Fail(LocalizationService.T("RtkStep_VerifyVersion_Failed"));
            },
        };

        var verifyGain = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_VerifyGain"),
            CommandPreview = "rtk gain",
            Run = async () =>
            {
                var result = await ExternalCommandRunner.RunAsync("rtk", new[] { "gain" }, TimeSpan.FromSeconds(15));
                return result.Started && result.ExitCode == 0
                    ? OperationStepResult.Ok(LocalizationService.T("RtkStep_VerifyGain_Ok"))
                    : OperationStepResult.Fail(LocalizationService.T("RtkStep_VerifyGain_Failed"));
            },
        };

        var initHook = new OperationStep
        {
            Description = LocalizationService.T("RtkStep_InitHook"),
            CommandPreview = "rtk init -g --auto-patch",
            Run = async () =>
            {
                // `rtk init -g` prompts "Patch existing ...settings.json? [y/N]" and hangs waiting
                // for input (confirmed by timeout on a real machine). `--auto-patch` is RTK's own
                // documented non-interactive flag (README: "Non-interactive (CI/CD)"); the "y\n" on
                // stdin is a defensive second layer in case some prompt still slips through. Timeout
                // is longer than the other rtk steps since this one does file I/O + a hook rewrite.
                var result = await ExternalCommandRunner.RunAsync(
                    "rtk", new[] { "init", "-g", "--auto-patch" }, TimeSpan.FromSeconds(120), standardInput: "y\n");

                if (result.Started && result.ExitCode == 0)
                {
                    return OperationStepResult.Ok(LocalizationService.T("RtkStep_InitHook_Ok"));
                }

                if (result.StandardError == "timeout")
                {
                    return OperationStepResult.Fail(LocalizationService.T("RtkStep_InitHook_Timeout"));
                }

                return OperationStepResult.Fail(LocalizationService.F("RtkStep_InitHook_Failed", result.StandardError));
            },
        };

        return new[] { fetchRelease, downloadAndExtract, cargoFallback, verifyVersion, verifyGain, initHook };
    }

    private static async Task<OperationStepResult> InstallRustupAsync()
    {
        var installerPath = Path.Combine(Path.GetTempPath(), "rustup-init-" + Guid.NewGuid() + ".exe");
        try
        {
            using (var http = new HttpClient())
            {
                var bytes = await http.GetByteArrayAsync(RustupInitUrl);
                await File.WriteAllBytesAsync(installerPath, bytes);
            }

            var result = await ExternalCommandRunner.RunAsync(installerPath, new[] { "-y" }, TimeSpan.FromMinutes(5));
            return result.Started && result.ExitCode == 0
                ? OperationStepResult.Ok(LocalizationService.T("RtkStep_Rustup_Ok"))
                : OperationStepResult.Fail(LocalizationService.T("RtkStep_Rustup_Failed"));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            return OperationStepResult.Fail(LocalizationService.F("RtkStep_Rustup_DownloadFailed", ex.Message));
        }
        finally
        {
            if (File.Exists(installerPath)) File.Delete(installerPath);
        }
    }

    private static void AddToPath(string directory)
    {
        const string variable = "PATH";
        var userPath = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) ?? "";
        if (!ContainsDirectory(userPath, directory))
        {
            var newUserPath = string.IsNullOrEmpty(userPath) ? directory : userPath + ";" + directory;
            Environment.SetEnvironmentVariable(variable, newUserPath, EnvironmentVariableTarget.User);
        }

        // Also update this process's own PATH so subsequent commands in the same run can find rtk immediately.
        var processPath = Environment.GetEnvironmentVariable(variable) ?? "";
        if (!ContainsDirectory(processPath, directory))
        {
            Environment.SetEnvironmentVariable(variable, processPath + ";" + directory);
        }
    }

    private static bool ContainsDirectory(string pathValue, string directory)
    {
        return pathValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(e => string.Equals(e.TrimEnd('\\'), directory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
    }
}
