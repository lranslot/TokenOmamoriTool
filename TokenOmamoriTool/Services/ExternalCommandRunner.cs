using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace TokenOmamoriTool.Services;

public static class ExternalCommandRunner
{
    public record Result(bool Started, int ExitCode, string StandardOutput, string StandardError);

    // `standardInput`, when given, is written and the pipe closed right after the process starts —
    // a defensive fallback for commands that may fall back to an interactive [y/N] confirmation
    // despite a non-interactive flag (see RtkInstaller's `rtk init -g --auto-patch` step). Harmless
    // no-op if the process never reads stdin.
    public static async Task<Result> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, string? standardInput = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments) startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new Result(false, -1, "", "");
        }

        if (standardInput is not null)
        {
            try
            {
                await process.StandardInput.WriteAsync(standardInput);
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // Process may have already exited or closed its stdin (e.g. the non-interactive
                // flag made the prompt unnecessary) — this input is a fallback, not a requirement.
            }
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            return new Result(true, -1, await stdoutTask, "timeout");
        }

        return new Result(true, process.ExitCode, await stdoutTask, await stderrTask);
    }
}
