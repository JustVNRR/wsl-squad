using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

using wsl_squad.Core.Repositories;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.Settings;
using wsl_squad.UI.Wpf.Views;

namespace wsl_squad.UI.Wpf.Services;

public sealed class WpfThemeService : IUiThemeService
{
    private static readonly string[] _builtIns = ["phosphor", "amber", "ice"];

    private readonly string _assetsRoot;
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private readonly ThemeService _palette;
    private readonly UiThemeSettings _settings;

    public WpfThemeService(string assetsRoot)
    {
        _assetsRoot = assetsRoot;
        _palette = new ThemeService(Path.Combine(assetsRoot, "themes"));
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(local, "wsl-squad", "gui-settings.json");
        _settings = Load();
        EnsureBuiltInThemes();
    }

    public UiThemeSettings GetTheme()
    {
        return _settings;
    }

    public void SetFontFamily(string family)
    {
        _settings.FontFamily = family;
        Save();
    }

    public void SetFontSize(int size)
    {
        _settings.FontSize = size;
        Save();
    }

    public void SetColorTheme(string colourTheme)
    {
        _settings.ColourSet = colourTheme;
        Save();
    }

    public void SetVariant(string variant)
    {
        _settings.ColourVariant = variant;
        Save();
    }

    public void Preview(string fontFamily, int fontSize, string colourSet, string variant)
    {
        _palette.Apply(colourSet, variant);
        ApplyLook(fontFamily, fontSize);
        RefreshFrames();
    }

    public IReadOnlyList<string> GetColorThemes()
    {
        string root = Path.Combine(_assetsRoot, "themes");
        HashSet<string> names = new(_builtIns, StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(root))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.xaml"))
            {
                _ = names.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        return [.. names.Order(StringComparer.OrdinalIgnoreCase)];
    }

    public IReadOnlyList<string> GetFontChoices()
    {
        List<string> names = [];

        try
        {
            ConfigRepository config = new(_assetsRoot);

            foreach (string name in config.GetFontNames())
            {
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }

            TerminalRepository terminal = new(_assetsRoot);

            foreach (TerminalFont font in terminal.GetTerminalFonts())
            {
                if (!names.Contains(font.Name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(font.Name);
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Trace.TraceWarning($"[WpfThemeService] Could not assemble font choices: {exception.Message}");
        }

        return names;
    }

    public string? UploadFont(string source)
    {
        try
        {
            string fingerprint = ShortHash(source);
            string folder = Path.Combine(
                _assetsRoot,
                "fonts",
                $"{Path.GetFileNameWithoutExtension(source)}-{fingerprint}");
            _ = Directory.CreateDirectory(folder);
            string target = Path.Combine(folder, Path.GetFileName(source));
            File.Copy(source, target, overwrite: true);

            Uri baseUri = new(folder + Path.DirectorySeparatorChar);
            FontFamily? family = Fonts.GetFontFamilies(baseUri)
                .FirstOrDefault(candidate => !candidate.Source.Contains("Font Awesome", StringComparison.OrdinalIgnoreCase));
            string? name = family?.FamilyNames.Values.FirstOrDefault();

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            FontFaceResolver.Register(target, name);

            return name;
        }
        catch (Exception exception) when (exception is IOException or FileFormatException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public string UploadTheme(string source)
    {
        string name = Path.GetFileNameWithoutExtension(source);
        string target = Path.Combine(_assetsRoot, "themes", $"{name}.xaml");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(target) ?? _assetsRoot);
        File.Copy(source, target, overwrite: true);

        return name;
    }

    public void DeleteTheme(string name)
    {
        string target = Path.Combine(_assetsRoot, "themes", $"{Path.GetFileNameWithoutExtension(name)}.xaml");

        if (!File.Exists(target))
        {
            throw new InvalidOperationException($"No theme named '{name}' lives in the themes folder.");
        }

        File.Delete(target);
    }

    public void OpenTheme(string name)
    {
        string target = Path.Combine(_assetsRoot, "themes", $"{Path.GetFileNameWithoutExtension(name)}.xaml");

        if (!File.Exists(target))
        {
            throw new InvalidOperationException($"No theme named '{name}' lives in the themes folder.");
        }

        _ = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    public void ExportTheme(string source, string target)
    {
        File.Copy(source, target, overwrite: true);
    }

    public string ThemePathFor(string name)
    {
        string chosen = string.IsNullOrEmpty(name) ? "phosphor" : name;

        return Path.Combine(_assetsRoot, "themes", $"{Path.GetFileNameWithoutExtension(chosen)}.xaml");
    }

    public string ThemeFileFor(string name)
    {
        return string.IsNullOrEmpty(name) ? "theme.xaml" : $"{Path.GetFileNameWithoutExtension(name)}.xaml";
    }

    public void ApplyPalette()
    {
        _palette.Apply(_settings.ColourSet, _settings.ColourVariant);
        RefreshFrames();
    }

    public void ApplyFace()
    {
        ApplyLook(_settings.FontFamily, _settings.FontSize);
    }

    private static void ApplyLook(string fontFamily, int fontSize)
    {
        ChromeWindow.UiFont = FontFaceResolver.Resolve(fontFamily);
        ChromeWindow.UiSize = fontSize;

        foreach (Window window in Application.Current.Windows)
        {
            window.FontFamily = ChromeWindow.UiFont;
            window.FontSize = ChromeWindow.UiSize;
        }

        if (Application.Current?.MainWindow is Window main)
        {
            main.FontFamily = ChromeWindow.UiFont;
            main.FontSize = ChromeWindow.UiSize;
        }
    }

    private static void RefreshFrames()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is ChromeWindow chrome)
            {
                chrome.RefreshFrame();
            }
        }
    }

    private void Save()
    {
        try
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath) ?? _assetsRoot);
            File.WriteAllText(
                _settingsPath,
                JsonSerializer.Serialize(_settings, _jsonOptions));
        }
        catch (IOException ex)
        {
            Trace.TraceWarning($"[WpfThemeService] Could not save GUI settings: {ex.Message}");
        }
    }

    private UiThemeSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                UiThemeSettings? saved = JsonSerializer.Deserialize<UiThemeSettings>(File.ReadAllText(_settingsPath));

                if (saved is not null && saved.FontSize > 0)
                {
                    return saved;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            Trace.TraceWarning($"[WpfThemeService] Could not load GUI settings, using defaults: {exception.Message}");
        }

        return new UiThemeSettings();
    }

    private void EnsureBuiltInThemes()
    {
        string root = Path.Combine(_assetsRoot, "themes");
        _ = Directory.CreateDirectory(root);

        foreach (string name in _builtIns)
        {
            string target = Path.Combine(root, $"{name}.xaml");

            if (IsXamlFile(target) && File.ReadAllText(target).Contains('\n', StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                ResourceDictionary compiled = new()
                {
                    Source = new Uri($"Themes/{name}.xaml", UriKind.Relative),
                };
                using FileStream output = File.Create(target);
                using XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    NewLineChars = Environment.NewLine,
                });
                XamlWriter.Save(compiled, writer);
            }
            catch (Exception exception) when (exception is IOException or XamlParseException or InvalidOperationException)
            {
                Trace.TraceWarning($"[WpfThemeService] Failed to generate built-in theme '{name}': {exception.Message}");

                try
                {
                    File.Delete(target);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Trace.TraceWarning($"[WpfThemeService] Could not delete invalid theme target '{target}': {ex.Message}");
                }
            }
        }
    }

    private static bool IsXamlFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return File.ReadAllText(path).TrimStart().StartsWith('<');
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[WpfThemeService] Could not read file to check XAML format '{path}': {ex.Message}");
            return false;
        }
    }

    private static string ShortHash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);

        return Convert.ToHexStringLower(hash)[..8];
    }
}
