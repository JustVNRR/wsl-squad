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

    private IReadOnlyDictionary<string, string> _strings = EnglishStrings.All;
    private IConfigRepository? _preferences;

    public Localizer()
    {
        _catalogs[English] = _strings;
        _displayNames[English] = "English";
        CurrentLanguage = English;

        foreach (LanguageFile file in LanguageFile.ReadEmbeddedLanguages())
        {
            if (file.Code is not null && file.Strings is not null)
            {
                RegisterLanguage(
                    new UiLanguage(
                        file.Code.Trim(),
                        file.DisplayName?.Trim() is { Length: > 0 } name ? name : file.Code.Trim()),
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

    public void LoadLanguages(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

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
            Dictionary<string, string> strings = new(StringComparer.Ordinal);

            if (_catalogs.TryGetValue(code, out IReadOnlyDictionary<string, string>? bundled))
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

            RegisterLanguage(
                new UiLanguage(code, file.DisplayName?.Trim() is { Length: > 0 } name ? name : code),
                strings);
        }
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
