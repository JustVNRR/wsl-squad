using wsl_squad.Presentation.Settings;

namespace wsl_squad.Presentation.Abstractions;

public interface IUiThemeService
{
    UiThemeSettings GetTheme();

    void SetFontFamily(string family);

    void SetFontSize(int size);

    void SetColorTheme(string colourTheme);

    void SetVariant(string variant);

    void Preview(string fontFamily, int fontSize, string colourSet, string variant);

    IReadOnlyList<string> GetColorThemes();

    IReadOnlyList<string> GetFontChoices();

    string? UploadFont(string source);

    string UploadTheme(string source);

    void DeleteTheme(string name);

    void OpenTheme(string name);

    void ExportTheme(string source, string target);

    string ThemePathFor(string name);

    string ThemeFileFor(string name);
}
