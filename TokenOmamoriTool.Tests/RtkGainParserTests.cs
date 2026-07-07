using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class RtkGainParserTests
{
    public RtkGainParserTests()
    {
        TestLocalization.UseJapanese(); // DisplayText is built through LocalizationService
    }

    [Fact]
    public void TryParse_SpecSampleOutput_ParsesCommandsAndSavings()
    {
        const string output = "Total commands: 998\nTokens saved: 2.2M (92.3%)\nTotal exec time: 23m22s (avg 1.4s)\n";

        var status = RtkGainParser.TryParse(output);

        Assert.NotNull(status);
        Assert.True(status!.ParsedOk);
        Assert.Equal(998, status.TotalCommands);
        Assert.Equal("2.2M", status.TokensSavedText);
        Assert.Equal(92.3, status.SavedPercent);
        Assert.Contains("2.2M", status.DisplayText);
        Assert.Contains("92.3", status.DisplayText);
    }

    [Fact]
    public void TryParse_UnrecognizedFormat_ReturnsNull()
    {
        var status = RtkGainParser.TryParse("rtk: command not found\n");

        Assert.Null(status);
    }

    [Fact]
    public void TryParse_EmptyOutput_ReturnsNull()
    {
        var status = RtkGainParser.TryParse("");

        Assert.Null(status);
    }
}
