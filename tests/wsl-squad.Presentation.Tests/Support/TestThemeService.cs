using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.Settings;

namespace wsl_squad.Presentation.Tests.Support;

public sealed class TestThemeService : IUiThemeService
{
    public UiThemeSettings Theme { get; set; } = new();

    public List<string> ColorThemes { get; set; } = ["phosphor", "amber"];

    public List<string> FontChoices { get; set; } = ["VT323", "MesloLGS NF"];

    public string? UploadFontFamily { get; set; } = "Cascadia Mono";

    public string? UploadedThemeName { get; set; } = "mine";

    public List<string> OpenedThemes { get; } = [];

    public List<string> DeletedThemes { get; } = [];

    public List<(string Source, string Target)> ExportedThemes { get; } = [];

    public List<(string FontFamily, int FontSize, string ColourSet, string Variant)> Previewed { get; } = [];

    public UiThemeSettings GetTheme()
    {
        return Theme;
    }

    public void SetFontFamily(string family)
    {
        Theme.FontFamily = family;
    }

    public void SetFontSize(int size)
    {
        Theme.FontSize = size;
    }

    public void SetColorTheme(string colourTheme)
    {
        Theme.ColourSet = colourTheme;
    }

    public void SetVariant(string variant)
    {
        Theme.ColourVariant = variant;
    }

    public void Preview(string fontFamily, int fontSize, string colourSet, string variant)
    {
        Previewed.Add((fontFamily, fontSize, colourSet, variant));
    }

    public IReadOnlyList<string> GetColorThemes()
    {
        return ColorThemes;
    }

    public IReadOnlyList<string> GetFontChoices()
    {
        return FontChoices;
    }

    public string? UploadFont(string source)
    {
        return UploadFontFamily;
    }

    public string UploadTheme(string source)
    {
        return UploadedThemeName ?? "mine";
    }

    public void DeleteTheme(string name)
    {
        DeletedThemes.Add(name);
    }

    public void OpenTheme(string name)
    {
        OpenedThemes.Add(name);
    }

    public void ExportTheme(string source, string target)
    {
        ExportedThemes.Add((source, target));
    }

    public string ThemePathFor(string name)
    {
        return $"/themes/{name}.xaml";
    }

    public string ThemeFileFor(string name)
    {
        return $"{name}.xaml";
    }
}
