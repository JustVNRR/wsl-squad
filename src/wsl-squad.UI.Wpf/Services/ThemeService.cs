using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace wsl_squad.UI.Wpf.Services;

internal sealed class ThemeService
{
    private static readonly string[] _knownSets = ["phosphor", "amber", "ice"];

    private readonly string? _themesRoot;
    private ResourceDictionary? _applied;

    public ThemeService(string? themesRoot = null)
    {
        _themesRoot = themesRoot;
    }

    public string ColourSet { get; private set; } = "phosphor";

    public string Variant { get; private set; } = "Dark";

    public static IReadOnlyList<string> ColourSets => _knownSets;

    public void Apply(string colourSet, string variant)
    {
        ResourceDictionary? palette = Load(colourSet);

        if (palette is null)
        {
            palette = Load("phosphor");
            colourSet = "phosphor";
        }

        ResourceDictionary chosen = palette?[variant] as ResourceDictionary
            ?? palette?["Dark"] as ResourceDictionary
            ?? [];

        Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;

        if (_applied is not null)
        {
            _ = merged.Remove(_applied);
        }

        merged.Add(chosen);
        _applied = chosen;
        ColourSet = colourSet;
        Variant = palette?.Contains(variant) == true ? variant : "Dark";
    }

    public void ToggleVariant()
    {
        string other = Variant == "Light" ? "Dark" : "Light";

        Apply(ColourSet, other);
    }

    private ResourceDictionary? Load(string colourSet)
    {
        try
        {
            if (!string.IsNullOrEmpty(_themesRoot))
            {
                string uploaded = Path.Combine(_themesRoot, $"{colourSet}.xaml");

                if (File.Exists(uploaded))
                {
                    try
                    {
                        return new ResourceDictionary
                        {
                            Source = new Uri(uploaded, UriKind.Absolute),
                        };
                    }
                    catch (XamlParseException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            return new ResourceDictionary
            {
                Source = new Uri($"Themes/{colourSet}.xaml", UriKind.Relative),
            };
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (XamlParseException)
        {
            return null;
        }
    }
}
