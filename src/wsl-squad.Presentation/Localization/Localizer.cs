using System.ComponentModel;
using System.Globalization;

using WslSquad.Core.Repositories;

namespace WslSquad.Presentation.Localization;

public sealed record UiLanguage(string Code, string DisplayName);

public sealed class Localizer : INotifyPropertyChanged
{
    public const string English = "en";

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _catalogs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _displayNames =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _bundledCatalogs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _bundledDisplayNames =
        new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, string> _strings = EnglishStrings.All;
    private IConfigRepository? _preferences;

    public Localizer()
    {
        CurrentLanguage = English;
        AddBundled(new UiLanguage(English, "English"), _strings);

        foreach (LanguageFile file in LanguageFile.ReadEmbeddedLanguages())
        {
            if (file.Code is not null && file.Strings is not null)
            {
                string code = file.Code.Trim();

                AddBundled(
                    new UiLanguage(
                        code,
                        file.DisplayName?.Trim() is { Length: > 0 } name ? name : code),
                    file.Strings);
            }
        }
    }

    public static Localizer Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage { get; private set; }

    public IReadOnlyList<UiLanguage> Languages =>
    [
        .. _displayNames
            .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new UiLanguage(pair.Key, pair.Value)),
    ];

    public string this[string key]
    {
        get
        {
            if (_strings.TryGetValue(key, out string? text))
            {
                return text;
            }

            return EnglishStrings.All.TryGetValue(key, out string? fallback) ? fallback : key;
        }
    }

    public string Format(string key, params object?[] arguments)
    {
        string template = this[key];

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, arguments);
        }
        catch (FormatException)
        {
            if (!EnglishStrings.All.TryGetValue(key, out string? english))
            {
                return template;
            }

            try
            {
                return string.Format(CultureInfo.CurrentCulture, english, arguments);
            }
            catch (FormatException)
            {
                return english;
            }
        }
    }

    public static UiLanguage? InspectLanguageFile(string path)
    {
        LanguageFile? file = LanguageFile.Read(path);
        string? code = file?.Code?.Trim();

        if (file is null || code is null || file.Strings is null || !IsUsableCode(code))
        {
            return null;
        }

        return new UiLanguage(
            code,
            file.DisplayName?.Trim() is { Length: > 0 } name ? name : code);
    }

    public void LoadLanguages(string directory)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> catalogs =
            new(_bundledCatalogs, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> displayNames =
            new(_bundledDisplayNames, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            foreach (string path in Directory.EnumerateFiles(directory)
                         .Where(file => Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
                         .Order(StringComparer.Ordinal))
            {
                LanguageFile? file = LanguageFile.Read(path);

                if (file?.Code is null)
                {
                    continue;
                }

                string code = file.Code.Trim();

                if (!IsUsableCode(code))
                {
                    continue;
                }

                Dictionary<string, string> strings = new(StringComparer.Ordinal);

                if (catalogs.TryGetValue(code, out IReadOnlyDictionary<string, string>? bundled))
                {
                    foreach (KeyValuePair<string, string> entry in bundled)
                    {
                        strings[entry.Key] = entry.Value;
                    }
                }

                if (file.Strings is not null)
                {
                    foreach (KeyValuePair<string, string> entry in file.Strings)
                    {
                        strings[entry.Key] = entry.Value;
                    }
                }

                catalogs[code] = strings;
                displayNames[code] = file.DisplayName?.Trim() is { Length: > 0 } name ? name : code;
            }
        }

        _catalogs.Clear();
        _displayNames.Clear();

        foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> entry in catalogs)
        {
            _catalogs[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<string, string> entry in displayNames)
        {
            _displayNames[entry.Key] = entry.Value;
        }

        bool switched = !_catalogs.ContainsKey(CurrentLanguage);

        if (switched)
        {
            CurrentLanguage = English;
            _preferences?.SaveLanguage(CurrentLanguage);
        }

        _strings = _catalogs[CurrentLanguage];
        RaiseTextChanged();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Languages)));

        if (switched)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        }
    }

    public IReadOnlyDictionary<string, string>? GetStrings(string code)
    {
        return _catalogs.TryGetValue(code, out IReadOnlyDictionary<string, string>? strings)
            ? strings
            : null;
    }

    public void UsePreferences(IConfigRepository preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
        string saved = preferences.GetLanguage();

        if (!string.IsNullOrWhiteSpace(saved))
        {
            SwitchLanguage(saved.Trim(), save: false);
        }
    }

    private static bool IsUsableCode(string code)
    {
        return code.Length is >= 2 and <= 16 &&
               code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private void AddBundled(UiLanguage language, IReadOnlyDictionary<string, string> strings)
    {
        _bundledCatalogs[language.Code] = strings;
        _bundledDisplayNames[language.Code] = language.DisplayName;
        _catalogs[language.Code] = strings;
        _displayNames[language.Code] = language.DisplayName;
    }

    public void RegisterLanguage(UiLanguage language, IReadOnlyDictionary<string, string> strings)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(strings);

        _catalogs[language.Code] = strings;
        _displayNames[language.Code] = language.DisplayName;

        if (string.Equals(CurrentLanguage, language.Code, StringComparison.OrdinalIgnoreCase))
        {
            _strings = strings;
            RaiseTextChanged();
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Languages)));
    }

    public void SetLanguage(string code)
    {
        SwitchLanguage(code, save: true);
    }

    private void SwitchLanguage(string code, bool save)
    {
        if (!_catalogs.TryGetValue(code, out IReadOnlyDictionary<string, string>? strings) ||
            string.Equals(CurrentLanguage, code, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _strings = strings;
        CurrentLanguage = code;

        if (save)
        {
            _preferences?.SaveLanguage(code);
        }

        RaiseTextChanged();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
    }

    private void RaiseTextChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
