using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class LocalizationServiceTests
{
    // --- NormalizeLanguage: §11.4 default/fallback rules ---

    [Theory]
    [InlineData("ja", false, "ja")]
    [InlineData("en", true, "en")]
    [InlineData(" JA ", false, "ja")] // tolerate case/whitespace noise in a hand-edited settings.json
    public void NormalizeLanguage_StoredSupportedValue_Wins(string stored, bool osJa, string expected)
    {
        Assert.Equal(expected, LocalizationService.NormalizeLanguage(stored, osJa));
    }

    [Theory]
    [InlineData(true, "ja")]
    [InlineData(false, "en")]
    public void NormalizeLanguage_FirstRun_FollowsOsUiCulture(bool osJa, string expected)
    {
        Assert.Equal(expected, LocalizationService.NormalizeLanguage(null, osJa));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("japanese")]
    [InlineData("ja-JP")]
    public void NormalizeLanguage_InvalidValue_FallsBackToEnglish(string stored)
    {
        // Even on a Japanese OS: an *invalid* stored value means en (§11.4), only a *missing* one
        // follows the OS culture.
        Assert.Equal("en", LocalizationService.NormalizeLanguage(stored, osUiCultureIsJapanese: true));
    }

    // --- Lookup: §11.5 missing-key fallback chain (current → en → key) ---

    private static readonly Dictionary<string, string> Current = new() { ["Both"] = "現在語", ["OnlyCurrent"] = "現在語のみ" };
    private static readonly Dictionary<string, string> Fallback = new() { ["Both"] = "english", ["OnlyFallback"] = "english only" };

    [Fact]
    public void Lookup_KeyInCurrentLanguage_ReturnsCurrentValue()
    {
        Assert.Equal("現在語", LocalizationService.Lookup("Both", Current, Fallback));
    }

    [Fact]
    public void Lookup_KeyMissingInCurrent_FallsBackToEnglish()
    {
        Assert.Equal("english only", LocalizationService.Lookup("OnlyFallback", Current, Fallback));
    }

    [Fact]
    public void Lookup_KeyMissingEverywhere_ReturnsTheKeyItself()
    {
        Assert.Equal("Nowhere", LocalizationService.Lookup("Nowhere", Current, Fallback));
    }
}
