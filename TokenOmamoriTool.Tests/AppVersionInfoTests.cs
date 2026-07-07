using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class AppVersionInfoTests
{
    [Fact]
    public void DisplayVersion_DropsTheFourthRevisionPart()
    {
        Assert.Equal("1.0.0", AppVersionInfo.DisplayVersion(new Version(1, 0, 0, 0)));
        Assert.Equal("2.13.4", AppVersionInfo.DisplayVersion(new Version(2, 13, 4, 99)));
    }

    [Fact]
    public void DisplayVersion_NullFallsBackToPlaceholder()
    {
        Assert.Equal("?", AppVersionInfo.DisplayVersion(null));
    }

    [Fact]
    public void CurrentDisplayVersion_ComesFromAssemblyMetadataNotHardcode()
    {
        // The csproj <Version> flows into the assembly version; whatever it is, the display
        // string must match it (spec §13.3: no hardcoded version anywhere).
        var expected = typeof(AppVersionInfo).Assembly.GetName().Version!;
        Assert.Equal($"{expected.Major}.{expected.Minor}.{expected.Build}", AppVersionInfo.CurrentDisplayVersion());
    }
}
