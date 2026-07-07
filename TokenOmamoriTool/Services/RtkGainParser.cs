using System.Globalization;
using System.Text.RegularExpressions;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

// Parses the plain-text output of `rtk gain`. Formatting may vary by version (per the spec's
// supplementary research doc, section 6-3), so callers must fall back to raw text on a null result.
public static class RtkGainParser
{
    private static readonly Regex CommandsPattern = new(@"Total commands:\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex SavedPattern = new(@"Tokens saved:\s*([\d.,]+[KMB]?)\s*\(([\d.]+)%\)", RegexOptions.Compiled);

    public static RtkGainStatus? TryParse(string output)
    {
        var savedMatch = SavedPattern.Match(output);
        if (!savedMatch.Success) return null;

        var commandsMatch = CommandsPattern.Match(output);
        var tokensSavedText = savedMatch.Groups[1].Value;
        var savedPercent = double.Parse(savedMatch.Groups[2].Value, CultureInfo.InvariantCulture);

        return new RtkGainStatus
        {
            Installed = true,
            ParsedOk = true,
            TotalCommands = commandsMatch.Success ? int.Parse(commandsMatch.Groups[1].Value, CultureInfo.InvariantCulture) : null,
            TokensSavedText = tokensSavedText,
            SavedPercent = savedPercent,
            RawOutput = output,
            DisplayText = LocalizationService.F("Rtk_Display", tokensSavedText, savedPercent),
        };
    }
}
