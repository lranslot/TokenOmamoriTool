using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace TokenOmamoriTool.Services;

/// <summary>
/// Runtime language switching (spec v0.3追補 §11). Holds the current language's strings both as a
/// merged ResourceDictionary (for DynamicResource references in XAML) and as plain dictionaries
/// (for code-behind lookups via T/F). English is always kept loaded as the fallback for missing
/// keys (§11.5). Switching raises LanguageChanged so dynamically-built UI (menus, tooltips,
/// monitor labels) can rebuild without a restart (§11.2).
/// </summary>
public static class LocalizationService
{
    public const string FallbackLanguage = "en";
    private static readonly string[] Supported = { "ja", "en" };

    public static string CurrentLanguage { get; private set; } = FallbackLanguage;
    public static event Action? LanguageChanged;

    private static IReadOnlyDictionary<string, string> _current = new Dictionary<string, string>();
    private static IReadOnlyDictionary<string, string> _fallback = new Dictionary<string, string>();
    private static ResourceDictionary? _mergedDictionary;

    /// <summary>
    /// Pure language resolution (§11.4): a stored "ja"/"en" wins; no stored value (first run)
    /// follows the OS UI culture; anything else falls back to English.
    /// </summary>
    public static string NormalizeLanguage(string? value, bool osUiCultureIsJapanese)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is not null && Supported.Contains(normalized)) return normalized;
        if (normalized is null || normalized.Length == 0) return osUiCultureIsJapanese ? "ja" : FallbackLanguage;
        return FallbackLanguage;
    }

    public static string ResolveInitialLanguage(string? configured) =>
        NormalizeLanguage(configured, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja");

    public static void ApplyLanguage(string language)
    {
        language = NormalizeLanguage(language, osUiCultureIsJapanese: false);
        var current = LoadResourceDictionary(language);
        var fallback = language == FallbackLanguage ? current : LoadResourceDictionary(FallbackLanguage);

        // Swap the merged dictionary so every {DynamicResource ...} in XAML updates immediately.
        if (Application.Current is not null)
        {
            var merged = Application.Current.Resources.MergedDictionaries;
            if (_mergedDictionary is not null) merged.Remove(_mergedDictionary);
            merged.Add(current);
            _mergedDictionary = current;
        }

        ApplyDictionaries(language, ToPlain(current), ToPlain(fallback));
    }

    /// <summary>
    /// Core state swap, separated from the WPF resource loading so unit tests can inject
    /// dictionaries parsed straight from the .xaml files without an Application instance.
    /// </summary>
    public static void ApplyDictionaries(
        string language,
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string> fallback)
    {
        CurrentLanguage = language;
        _current = current;
        _fallback = fallback;
        LanguageChanged?.Invoke();
    }

    public static string T(string key) => Lookup(key, _current, _fallback);

    public static string F(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    /// <summary>
    /// Pure lookup with the §11.5 fallback chain: current language → English → the key itself.
    /// Misses are reported to the Debug log only, never shown to the user.
    /// </summary>
    public static string Lookup(
        string key,
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string> fallback)
    {
        if (current.TryGetValue(key, out var value)) return value;
        if (fallback.TryGetValue(key, out var fallbackValue))
        {
            Debug.WriteLine($"[Localization] Key '{key}' missing in '{CurrentLanguage}'; using the English fallback.");
            return fallbackValue;
        }
        Debug.WriteLine($"[Localization] Key '{key}' missing in every dictionary.");
        return key;
    }

    private static ResourceDictionary LoadResourceDictionary(string language) => new()
    {
        Source = new Uri($"pack://application:,,,/Resources/Strings.{language}.xaml"),
    };

    private static IReadOnlyDictionary<string, string> ToPlain(ResourceDictionary dictionary)
    {
        var plain = new Dictionary<string, string>();
        foreach (var key in dictionary.Keys)
        {
            if (key is string name && dictionary[name] is string value)
            {
                plain[name] = value;
            }
        }
        return plain;
    }
}
