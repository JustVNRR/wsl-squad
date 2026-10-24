using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed record LookPairRow(WslIconPairDto Pair, string Label);

public sealed record LookFontRow(string Name, string Label);

public sealed record LookSchemeRow(WslColorSchemeDto Scheme, string Label);

public sealed partial class SetAppearanceViewModel : BaseViewModel
{
    private string SourceName { get; }
    private readonly GetAppearanceResponseDto _appearance;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    public partial string IconText { get; set; }

    [ObservableProperty]
    public partial string IconError { get; set; }

    [ObservableProperty]
    public partial int PairIndex { get; set; }

    [ObservableProperty]
    public partial int FontIndex { get; set; }

    [ObservableProperty]
    public partial int SchemeIndex { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; }

    public SetAppearanceViewModel(
        string name,
        GetAppearanceResponseDto appearance,
        IReadOnlyList<string> fonts,
        IReadOnlyList<WslColorSchemeDto> schemes,
        IReadOnlyList<WslIconPairDto> pairs,
        IDialogService dialogs)
    {
        InstanceName = name;
        SourceName = name;
        _appearance = appearance;
        _dialogs = dialogs;
        IconError = string.Empty;
        ImagePath = string.Empty;
        ApplyCommand = new RelayCommand(Apply, () => IconError.Length == 0);
        PickImageCommand = new RelayCommand(PickImage);
        IconText = appearance.Icon?.Text ?? string.Empty;

        List<LookPairRow> pairRows = [];
        foreach (WslIconPairDto pair in pairs)
        {
            bool current = appearance.Icon is not null &&
                           pair.Top == appearance.Icon.Top &&
                           pair.Bottom == appearance.Icon.Bottom;
            pairRows.Add(new LookPairRow(pair, current ? $"{pair.Name}{Localizer.Instance["CurrentSuffix"]}" : pair.Name ?? string.Empty));
        }

        Pairs = pairRows;
        PairLabels = [.. pairRows.Select(row => row.Label)];
        PairIndex = pairRows.FindIndex(row => row.Label.EndsWith("(current)", StringComparison.Ordinal));
        PairIndex = Math.Max(0, PairIndex);

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
                string.Equals(font, appearance.Font, StringComparison.Ordinal) ? $"{font}{Localizer.Instance["CurrentSuffix"]}" : font));
        }

        Fonts = fontRows;
        FontLabels = [.. fontRows.Select(row => row.Label)];
        FontIndex = Math.Max(0, fontRows.FindIndex(row =>
            string.Equals(row.Name, appearance.Font, StringComparison.Ordinal)));

        List<LookSchemeRow> schemeRows = [];
        foreach (WslColorSchemeDto scheme in schemes.OrderBy(scheme => scheme.Name, StringComparer.Ordinal))
        {
            schemeRows.Add(new LookSchemeRow(
                scheme,
                string.Equals(scheme.Name, appearance.ColourScheme, StringComparison.Ordinal)
                    ? $"{scheme.Name}{Localizer.Instance["CurrentSuffix"]}"
                    : scheme.Name ?? string.Empty));
        }

        Schemes = schemeRows;
        SchemeLabels = [.. schemeRows.Select(row => row.Label)];
        SchemeIndex = Math.Max(0, schemeRows.FindIndex(row =>
            string.Equals(row.Scheme.Name, appearance.ColourScheme, StringComparison.Ordinal)));

        if (!string.IsNullOrEmpty(appearance.IconPath) && File.Exists(appearance.IconPath))
        {
            ImagePath = appearance.IconPath;
        }

        ValidateIcon();
    }

    public string InstanceName { get; }

    public string Lead => Localizer.Instance.Format("LeadAppearance", SourceName).ToUpperInvariant();

    public IReadOnlyList<LookPairRow> Pairs { get; }

    public IReadOnlyList<string> PairLabels { get; }

    public IReadOnlyList<LookFontRow> Fonts { get; }

    public IReadOnlyList<string> FontLabels { get; }

    public IReadOnlyList<LookSchemeRow> Schemes { get; }

    public IReadOnlyList<string> SchemeLabels { get; }

    public SetAppearanceDto? Result { get; private set; }

    public IRelayCommand ApplyCommand { get; }

    public IRelayCommand PickImageCommand { get; }

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

    private void PickImage()
    {
        string? path = _dialogs.PickOpenFile(
            Localizer.Instance.Format("FileDialogIcon", InstanceName),
            Localizer.Instance["FileFilterImages"]);

        if (path is not null)
        {
            SetImage(path);
        }
    }

    private void ValidateIcon()
    {
        string text = IconText.Trim();
        bool valid = text.Length is >= 1 and <= 3 && text.All(char.IsAsciiLetterOrDigit);

        IconError = valid ? string.Empty : Localizer.Instance["ErrorIconFormat"];
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
