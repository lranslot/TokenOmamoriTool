using System.Text.RegularExpressions;

namespace TokenOmamoriTool.Tests;

// Spec §11.6: the ja/en key sets must always match, so a key added to one dictionary but not the
// other fails CI instead of silently falling back at runtime.
public class LocalizationDictionaryTests
{
    private static readonly Regex Placeholder = new(@"\{(\d+)", RegexOptions.Compiled);

    [Fact]
    public void JaAndEn_KeySetsMatch()
    {
        var ja = TestLocalization.LoadStrings("ja");
        var en = TestLocalization.LoadStrings("en");

        var missingInEn = ja.Keys.Except(en.Keys).OrderBy(k => k).ToList();
        var missingInJa = en.Keys.Except(ja.Keys).OrderBy(k => k).ToList();

        Assert.True(missingInEn.Count == 0 && missingInJa.Count == 0,
            $"Missing in en: [{string.Join(", ", missingInEn)}] / Missing in ja: [{string.Join(", ", missingInJa)}]");
    }

    [Fact]
    public void AllValues_AreNonEmpty()
    {
        foreach (var language in new[] { "ja", "en" })
        {
            foreach (var (key, value) in TestLocalization.LoadStrings(language))
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"Empty value for '{key}' in {language}");
            }
        }
    }

    [Fact]
    public void FormatPlaceholders_MatchBetweenLanguages()
    {
        var ja = TestLocalization.LoadStrings("ja");
        var en = TestLocalization.LoadStrings("en");

        foreach (var key in ja.Keys.Intersect(en.Keys))
        {
            var jaArgs = Placeholder.Matches(ja[key]).Select(m => m.Groups[1].Value).OrderBy(v => v);
            var enArgs = Placeholder.Matches(en[key]).Select(m => m.Groups[1].Value).OrderBy(v => v);
            Assert.True(jaArgs.SequenceEqual(enArgs),
                $"Placeholder mismatch for '{key}': ja has {{{string.Join(",", jaArgs)}}}, en has {{{string.Join(",", enArgs)}}}");
        }
    }
}
