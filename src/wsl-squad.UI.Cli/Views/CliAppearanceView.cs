using WslSquad.Core.Dtos;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli.Interaction;

namespace WslSquad.UI.Cli.Views;

public sealed class CliAppearanceView : CliViewBase, IAppearanceView
{
    public CliAppearanceView(ConsoleHost host)
        : base(host)
    {
    }

    public void Clear()
    {
        Host.Clear();
    }

    public void ShowNoInstances()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliNoInstance"], ConsoleTone.Error);
        Write(Localizer.Instance["CliBuildOne"], ConsoleTone.Hint);
    }

    public string? AskOffered(IReadOnlyList<WslIndexDto> instances)
    {
        WslIndexDto? picked = Pick(
            Localizer.Instance["CliOurInstances"],
            instances,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public string? AskTheme(string name)
    {
        List<Choice> choices =
        [
            new Choice("icon", Localizer.Instance["CliTile"], Localizer.Instance["CliChoiceIcon"]),
            new Choice("font", Localizer.Instance["CliWholeTerminal"], Localizer.Instance["CliChoiceFont"]),
            new Choice("color", Localizer.Instance["CliBackground"], Localizer.Instance["CliChoiceColor"]),
        ];
        int labelWidth = choices.Max(choice => choice.Name.Length);

        return Pick(
            Localizer.Instance.Format("CliThemeOf", name),
            choices,
            choice => $"{choice.Name.PadRight(labelWidth)} {choice.Description}")?.What;
    }

    public string? AskFont(string name, IReadOnlyList<string> fonts, string current)
    {
        return Pick(
            Localizer.Instance.Format("CliFontOf", name),
            fonts,
            font => string.Equals(font, current, StringComparison.Ordinal)
                ? $"{font,-33}{Localizer.Instance["CurrentSuffix"]}"
                : font,
            Localizer.Instance["FontsHint"]);
    }

    public string? AskColour(string name, IReadOnlyList<WslColorSchemeDto> schemes, string current)
    {
        return Pick(
            Localizer.Instance.Format("CliColoursOf", name),
            [.. schemes.OrderBy(scheme => scheme.Name, StringComparer.Ordinal)],
            scheme => string.Equals(scheme.Name, current, StringComparison.Ordinal)
                ? $"{scheme.Name,-20}{Localizer.Instance["CurrentSuffix"]}"
                : $"{scheme.Name,-20}")?.Name;
    }

    public string? AskIconType(string name)
    {
        return Pick(
            Localizer.Instance.Format("CliIconOf", name),
            [
                new Choice("auto", Localizer.Instance["CliByDefault"]),
                new Choice("letters", Localizer.Instance["CliText"]),
                new Choice("colours", Localizer.Instance["CliColors"]),
                new Choice("image", Localizer.Instance["CliLocalFile"]),
            ],
            choice => choice.Description)?.What;
    }

    public string? AskLetters(string question)
    {
        return AskLine(question)?.Trim();
    }

    public void ShowLettersHint()
    {
        Write(Localizer.Instance["CliLettersHint"], ConsoleTone.Hint);
    }

    public WslIconPairDto? AskIconPair(
        string name,
        IReadOnlyList<WslIconPairDto> pairs,
        string suggestion,
        string? top,
        string? bottom)
    {
        return Pick(
            Localizer.Instance.Format("CliColoursFor", name),
            pairs,
            pair =>
            {
                bool current = pair.Top == top && pair.Bottom == bottom;

                return $"{suggestion,-4} {pair.Name,-9}{(current ? Localizer.Instance["CurrentSuffix"] : string.Empty)}";
            });
    }

    public string? AskImagePath()
    {
        return AskLine(Localizer.Instance["CliImagePath"])?.Trim('"');
    }

    public void ShowNoFile()
    {
        Write(Localizer.Instance["CliNoFileAtPath"], ConsoleTone.Warning);
    }

    public void ShowBadExtension()
    {
        Write(Localizer.Instance["CliBadImageExtension"], ConsoleTone.Hint);
    }

    public void Show(SetAppearanceResponseDto response)
    {
        Write(string.Empty);
        Write(
            Localizer.Instance.Format(
                "CliThemeApplied",
                response.InstanceName,
                string.Join(", ", response.Changes.Select(change => change.ToMessage()))),
            ConsoleTone.Success);
        Write(string.Empty);
    }

    private sealed record Choice(string What, string Description, string? Label = null)
    {
        public string Name => Label ?? What;
    }
}
