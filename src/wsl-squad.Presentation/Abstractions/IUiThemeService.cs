
namespace wsl_squad.Presentation.Abstractions;

public interface IUiThemeService
{
    UiThemeSettings GetTheme();

    void Preview(UiThemeSettings settings);

    void ApplyTheme(UiThemeSettings settings);

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
