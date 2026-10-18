using wsl_squad.Presentation.Settings;

namespace wsl_squad.Presentation.Abstractions;

public interface IUiThemeService
{
    public UiThemeSettings GetTheme();

    public void SetFontFamily(string family);

    public void SetFontSize(int size);

    public void SetColorTheme(string colourTheme);

    public void SetVariant(string variant);

    public void Preview(string fontFamily, int fontSize, string colourSet, string variant);

    public IReadOnlyList<string> GetColorThemes();

    public IReadOnlyList<string> GetFontChoices();

    public string? UploadFont(string source);

    public string UploadTheme(string source);

    public void DeleteTheme(string name);

    public void OpenTheme(string name);

    public void ExportTheme(string source, string target);

    public string ThemePathFor(string name);

    public string ThemeFileFor(string name);
}
