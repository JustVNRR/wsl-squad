using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace WslSquad.UI.Wpf.Services;

public static class FontFaceResolver
{
    private static readonly FontFamily _fallback = new("Segoe UI");
    private static readonly Dictionary<string, FontFamily> _registered = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> CatalogNames =>
        [.. _registered.Keys.Order(StringComparer.OrdinalIgnoreCase)];

    public static void LoadCatalog(string assetsRoot)
    {
        string root = Path.Combine(assetsRoot, "fonts");

        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            try
            {
                Uri baseUri = new(directory + Path.DirectorySeparatorChar);
                foreach (FontFamily family in Fonts.GetFontFamilies(baseUri))
                {
                    string? name = family.FamilyNames.Values.FirstOrDefault();

                    if (string.IsNullOrEmpty(name) ||
                        name.StartsWith("Font Awesome", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _registered[name] = family;
                }
            }
            catch (Exception exception) when (exception is IOException or FileFormatException)
            {
                Trace.TraceWarning($"[FontFaceResolver] Skipped invalid font directory '{directory}': {exception.Message}");
            }
        }
    }

    public static void Register(string filePath, string family)
    {
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;

        _registered[family] = new FontFamily(
            new Uri(directory + Path.DirectorySeparatorChar),
            $"./#{family}");
    }

    public static FontFamily Resolve(string? family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            return _fallback;
        }

        if (_registered.TryGetValue(family, out FontFamily? registered))
        {
            return registered;
        }

        return new FontFamily(family);
    }
}
