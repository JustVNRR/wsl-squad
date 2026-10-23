using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Gui.ViewModels;

public sealed record LookPairRow(WslIconPairDto Pair, string Label);

public sealed record LookFontRow(string Name, string Label);

public sealed record LookSchemeRow(WslColorSchemeDto Scheme, string Label);

public sealed partial class SetAppearanceViewModel : BaseViewModel
{
    private readonly GetAppearanceResponseDto _appearance;

    [ObservableProperty]
    private string _iconText;

    [ObservableProperty]
    private string _iconError = string.Empty;

    [ObservableProperty]
    private int _pairIndex;

    [ObservableProperty]
    private int _fontIndex;

    [ObservableProperty]
    private int _schemeIndex;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    public SetAppearanceViewModel(
        string name,
        GetAppearanceResponseDto appearance,
        IReadOnlyList<string> fonts,
        IReadOnlyList<WslColorSchemeDto> schemes,
        IReadOnlyList<WslIconPairDto> pairs)
    {
        InstanceName = name;
        _appearance = appearance;
        Lead = $"{name}'s appearance.".ToUpperInvariant();

        _iconText = appearance.Icon?.Text ?? string.Empty;

        List<LookPairRow> pairRows = [];
        foreach (WslIconPairDto pair in pairs)
        {
            bool current = appearance.Icon is not null &&
                           pair.Top == appearance.Icon.Top &&
                           pair.Bottom == appearance.Icon.Bottom;
            pairRows.Add(new LookPairRow(pair, current ? $"{pair.Name}  (current)" : pair.Name ?? string.Empty));
        }

        Pairs = pairRows;
        PairLabels = [.. pairRows.Select(row => row.Label)];
        _pairIndex = pairRows.FindIndex(row => row.Label.EndsWith("(current)", StringComparison.Ordinal));
        _pairIndex = Math.Max(0, _pairIndex);

        List<LookFontRow> fontRows = [];
        List<string> fontNames = [.. fonts];

        if (!string.IsNullOrEmpty(appearance.Font) &&
            !fontNames.Contains(appearance.Font, StringComparer.Ordinal))
        {
            fontNames.Insert(0, appearance.Font);
        }

        foreach (string font in fontNames)
        {
            fontRows.Add(new LookFontRow(
                font,
                string.Equals(font, appearance.Font, StringComparison.Ordinal) ? $"{font}  (current)" : font));
        }

        Fonts = fontRows;
        FontLabels = [.. fontRows.Select(row => row.Label)];
        _fontIndex = Math.Max(0, fontRows.FindIndex(row =>
            string.Equals(row.Name, appearance.Font, StringComparison.Ordinal)));

        List<LookSchemeRow> schemeRows = [];
        foreach (WslColorSchemeDto scheme in schemes.OrderBy(scheme => scheme.Name, StringComparer.Ordinal))
        {
            schemeRows.Add(new LookSchemeRow(
                scheme,
                string.Equals(scheme.Name, appearance.ColourScheme, StringComparison.Ordinal)
                    ? $"{scheme.Name}  (current)"
                    : scheme.Name ?? string.Empty));
        }

        Schemes = schemeRows;
        SchemeLabels = [.. schemeRows.Select(row => row.Label)];
        _schemeIndex = Math.Max(0, schemeRows.FindIndex(row =>
            string.Equals(row.Scheme.Name, appearance.ColourScheme, StringComparison.Ordinal)));

        if (!string.IsNullOrEmpty(appearance.IconPath) && File.Exists(appearance.IconPath))
        {
            _imagePath = appearance.IconPath;
        }

        ApplyCommand = new RelayCommand(Apply, () => IconError.Length == 0);
        ValidateIcon();
    }

    public string InstanceName { get; }

    public string Lead { get; }

    public IReadOnlyList<LookPairRow> Pairs { get; }

    public IReadOnlyList<string> PairLabels { get; }

    public IReadOnlyList<LookFontRow> Fonts { get; }

    public IReadOnlyList<string> FontLabels { get; }

    public IReadOnlyList<LookSchemeRow> Schemes { get; }

    public IReadOnlyList<string> SchemeLabels { get; }

    public SetAppearanceDto? Result { get; private set; }

    public IRelayCommand ApplyCommand { get; }

    public string SelectedFontName =>
        Fonts.Count == 0 ? _appearance.Font ?? string.Empty : Fonts[Math.Clamp(FontIndex, 0, Fonts.Count - 1)].Name;

    public LookPairRow? SelectedPair =>
        Pairs.Count == 0 ? null : Pairs[Math.Clamp(PairIndex, 0, Pairs.Count - 1)];

    public WslColorSchemeDto? SelectedScheme =>
        Schemes.Count == 0 ? null : Schemes[Math.Clamp(SchemeIndex, 0, Schemes.Count - 1)].Scheme;

    partial void OnIconTextChanged(string value)
    {
        ImagePath = string.Empty;
        ValidateIcon();
    }

    partial void OnPairIndexChanged(int value)
    {
        ImagePath = string.Empty;
    }

    public void SetImage(string path)
    {
        ImagePath = path;
    }

    private void ValidateIcon()
    {
        string text = IconText.Trim();
        bool valid = text.Length is >= 1 and <= 3 && text.All(char.IsAsciiLetterOrDigit);

        IconError = valid ? string.Empty : "One to three letters or digits.";
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private void Apply()
    {
        if (!CanApply())
        {
            return;
        }

        WslIconPairDto? pair = Pairs.Count == 0 ? null : Pairs[Math.Clamp(PairIndex, 0, Pairs.Count - 1)].Pair;
        WslIconDto icon = new()
        {
            Text = IconText.Trim().ToUpperInvariant(),
            Top = pair?.Top ?? _appearance.Icon?.Top ?? string.Empty,
            Bottom = pair?.Bottom ?? _appearance.Icon?.Bottom ?? string.Empty,
            TextColor = pair?.TextColor ?? _appearance.Icon?.TextColor ?? string.Empty,
        };

        Result = new SetAppearanceDto
        {
            InstanceName = InstanceName,
            Font = SelectedFontName,
            ColourScheme = SelectedScheme?.Name ?? _appearance.ColourScheme ?? string.Empty,
            Icon = icon,
            IconImage = ImagePath,
        };
        RequestClose();
    }

    private bool CanApply()
    {
        return IconError.Length == 0;
    }
}
