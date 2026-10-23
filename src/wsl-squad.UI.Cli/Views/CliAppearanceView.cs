using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.UI.Cli.Views;

public sealed class CliAppearanceView : CliViewBase, IAppearanceView
{
    public void ShowNoInstances()
    {
        Write(string.Empty);
        Write("[ABORT] No instance of this template is registered on this machine.", ConsoleTone.Error);
        Write("        Build one with  wsl-squad build", ConsoleTone.Hint);
    }

    public string? AskOffered(IReadOnlyList<WslIndexDto> instances)
    {
        WslIndexDto? picked = Pick(
            "Our Instances",
            instances,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public string? AskTheme(string name)
    {
        return Pick(
            $"Theme of '{name}'",
            [
                new Choice("icon", "the tile in the tab"),
                new Choice("font", "what the whole terminal is written in"),
                new Choice("color", "the background, the text, and sixteen colours"),
            ],
            choice => $"{choice.What,-6} {choice.About}")?.What;
    }

    public string? AskFont(string name, IReadOnlyList<string> fonts, string current)
    {
        return Pick(
            $"Font of '{name}'",
            fonts,
            font => string.Equals(font, current, StringComparison.Ordinal)
                ? $"{font,-33} (current)"
                : font,
            "Get more Nerd Fonts at https://www.nerdfonts.com");
    }

    public string? AskColour(string name, IReadOnlyList<WslColorSchemeDto> schemes, string current)
    {
        return Pick(
            $"Colours of '{name}'",
            [.. schemes.OrderBy(scheme => scheme.Name, StringComparer.Ordinal)],
            scheme => string.Equals(scheme.Name, current, StringComparison.Ordinal)
                ? $"{scheme.Name,-20} (current)"
                : $"{scheme.Name,-20}")?.Name;
    }

    public string? AskIconType(string name)
    {
        return Pick(
            $"Icon of '{name}'",
            [
                new Choice("auto", "By Default"),
                new Choice("letters", "text"),
                new Choice("colours", "colors"),
                new Choice("image", "local file"),
            ],
            choice => choice.About)?.What;
    }

    public string? AskLetters(string question)
    {
        return AskLine(question)?.Trim();
    }

    public void ShowLettersHint()
    {
        Write("  One to three letters or digits.", ConsoleTone.Hint);
    }

    public WslIconPairDto? AskIconPair(
        string name,
        IReadOnlyList<WslIconPairDto> pairs,
        string suggestion,
        string? top,
        string? bottom)
    {
        return Pick(
            $"Colours for '{name}'",
            pairs,
            pair =>
            {
                bool current = pair.Top == top && pair.Bottom == bottom;

                return $"{suggestion,-4} {pair.Name,-9}{(current ? "  (current)" : string.Empty)}";
            });
    }

    public string? AskImagePath()
    {
        return AskLine("Path of the image, PNG JPG ICO or BMP (CTRL+C to abort)")?.Trim('"');
    }

    public void ShowNoFile()
    {
        Write("  No file at that path.", ConsoleTone.Warning);
    }

    public void ShowBadExtension()
    {
        Write("  The name has to end in .png, .jpg, .jpeg, .ico or .bmp.", ConsoleTone.Hint);
    }

    public void Show(SetAppearanceResponseDto response)
    {
        Write(string.Empty);
        Write($"==> Theme of '{response.InstanceName}': {string.Join(", ", response.Changes)}", ConsoleTone.Success);
        Write(string.Empty);
    }

    private sealed record Choice(string What, string About);
}
