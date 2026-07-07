using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class RtkGainRunner
{
    public static async Task<RtkGainStatus> RunAsync()
    {
        var result = await ExternalCommandRunner.RunAsync("rtk", new[] { "gain" }, TimeSpan.FromSeconds(15));

        if (!result.Started)
        {
            return new RtkGainStatus { Installed = false, DisplayText = LocalizationService.T("Rtk_NotInstalled") };
        }

        var parsed = RtkGainParser.TryParse(result.StandardOutput);
        if (parsed is not null) return parsed;

        return new RtkGainStatus
        {
            Installed = true,
            ParsedOk = false,
            RawOutput = result.StandardOutput,
            DisplayText = string.IsNullOrWhiteSpace(result.StandardOutput)
                ? LocalizationService.T("Rtk_NoOutput")
                : LocalizationService.F("Rtk_Raw", result.StandardOutput),
        };
    }
}
