using System.Diagnostics;
using System.IO;
using System.Text.Json;

using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Localization;

namespace WslSquad.UI.Wpf.Services;

public sealed class WpfLanguageService : IUiLanguageService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private readonly string _languagesRoot;

    public WpfLanguageService(string assetsRoot)
    {
        _languagesRoot = Path.Combine(assetsRoot, "languages");
    }

    public string? LanguageCodeFor(string source)
    {
        return Localizer.InspectLanguageFile(source)?.Code;
    }

    public string UploadLanguage(string source)
    {
        UiLanguage language = Localizer.InspectLanguageFile(source)
            ?? throw new InvalidOperationException(Localizer.Instance["ErrorNotALanguage"]);

        string target = PathFor(language.Code);
        Directory.CreateDirectory(_languagesRoot);

        if (!string.Equals(
                Path.GetFullPath(source),
                Path.GetFullPath(target),
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(source, target, overwrite: true);
        }

        Localizer.Instance.LoadLanguages(_languagesRoot);

        return language.Code;
    }

    public void DeleteLanguage(string code)
    {
        string target = PathFor(code);

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        Localizer.Instance.LoadLanguages(_languagesRoot);
    }

    public void OpenLanguage(string code)
    {
        Process.Start(new ProcessStartInfo(EnsureFile(code)) { UseShellExecute = true });
    }

    public void ExportLanguage(string code, string target)
    {
        File.Copy(EnsureFile(code), target, overwrite: true);
    }

    public string LanguageFileFor(string code)
    {
        return $"{code}.json";
    }

    private string EnsureFile(string code)
    {
        string target = PathFor(code);

        if (File.Exists(target))
        {
            return target;
        }

        IReadOnlyDictionary<string, string>? strings = Localizer.Instance.GetStrings(code);
        UiLanguage? language = Localizer.Instance.Languages
            .FirstOrDefault(candidate => string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase));

        if (strings is null || language is null)
        {
            throw new InvalidOperationException(
                Localizer.Instance.Format("ErrorLanguageMissing", code));
        }

        Directory.CreateDirectory(_languagesRoot);
        File.WriteAllText(
            target,
            JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["code"] = language.Code,
                    ["displayName"] = language.DisplayName,
                    ["strings"] = strings,
                },
                _jsonOptions));

        return target;
    }

    private string PathFor(string code)
    {
        return Path.Combine(_languagesRoot, $"{code}.json");
    }
}
