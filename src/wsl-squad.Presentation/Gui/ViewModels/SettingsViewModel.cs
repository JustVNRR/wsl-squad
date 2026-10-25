using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed record SettingsRow(string Name, string Label);

public sealed record SettingsPackRow(
    string Name,
    string Family,
    string Description,
    string Label,
    IReadOnlyList<string> Dependents);

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly IUiThemeService _ui;
    private readonly IPackCatalogService _catalogue;
    private readonly FleetWorkflow _fleetWorkflow;
    private readonly IDialogService _dialogs;
    private string? _newRoot;

    [ObservableProperty]
    public partial int FontIndex { get; set; }

    [ObservableProperty]
    public partial int SizeIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedColour))]
    public partial int ColourIndex { get; set; }

    [ObservableProperty]
    public partial int PackIndex { get; set; }

    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    [ObservableProperty]
    public partial string Root { get; set; }

    [ObservableProperty]
    public partial string Variant { get; set; }

    [ObservableProperty]
    public partial string ThemeError { get; set; }

    [ObservableProperty]
    public partial string CatalogueError { get; set; }

    [ObservableProperty]
    public partial string ConfigError { get; set; }

    [ObservableProperty]
    public partial bool CanTheme { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenPackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicatePackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePackCommand))]
    public partial bool CanPack { get; set; }

    public SettingsViewModel(
        IUiThemeService ui,
        IPackCatalogService catalogue,
        IFleetService fleet,
        IDialogService dialogs)
    {
        _ui = ui;
        _catalogue = catalogue;
        _fleetWorkflow = new FleetWorkflow(fleet);
        _dialogs = dialogs;
        ThemeError = string.Empty;
        CatalogueError = string.Empty;
        ConfigError = string.Empty;

        UiThemeSettings theme = ui.GetTheme();
        string variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;

        try
        {
            Root = fleet.GetRoot();
        }
        catch (InvalidOperationException)
        {
            Root = string.Empty;
        }

        WslConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig");

        List<string> themes = [.. ui.GetColorThemes()];
        List<string> fonts = [.. ui.GetFontChoices()];

        if (!fonts.Contains(theme.FontFamily, StringComparer.Ordinal))
        {
            fonts.Insert(0, theme.FontFamily);
        }

        Fonts = [.. fonts.Select(name => new SettingsRow(name, name))];
        int fontIndex = Math.Max(0, Fonts.FindIndex(row => string.Equals(row.Name, theme.FontFamily, StringComparison.Ordinal)));

        List<int> sizes = [11, 13, 15, 17, 19];

        if (!sizes.Contains(theme.FontSize))
        {
            sizes.Insert(0, theme.FontSize);
        }

        Sizes = [.. sizes.Select(size => new SettingsRow(
            size.ToString(System.Globalization.CultureInfo.InvariantCulture),
            size.ToString(System.Globalization.CultureInfo.InvariantCulture)))];
        int sizeIndex = Math.Max(0, Sizes.FindIndex(row =>
            int.TryParse(row.Name, out int size) && size == theme.FontSize));

        List<string> sets = [];
        if (!themes.Contains(theme.ColourSet, StringComparer.Ordinal))
        {
            sets.Add(theme.ColourSet);
        }

        sets.AddRange(themes);

        Colours = [.. sets.Select(name => new SettingsRow(name, name.Length == 0 ? Localizer.Instance["DefaultOption"] : name))];
        int colourIndex = Math.Max(0, Colours.FindIndex(row =>
            string.Equals(row.Name, theme.ColourSet, StringComparison.Ordinal)));

        IReadOnlyList<WslPackDto> packs;

        try
        {
            packs = catalogue.GetAllPacks();
        }
        catch (InvalidOperationException)
        {
            packs = [];
        }

        Packs = PackRowsOf(packs);
        int packIndex = Packs.Count > 0 ? 0 : -1;

        CloseCommand = new RelayCommand(Close);
        EditRootCommand = new RelayCommand(EditRoot);
        WslConfigCommand = new RelayCommand(WslConfig);
        OpenPackCommand = new RelayCommand(OpenPack, () => CanPack);
        DuplicatePackCommand = new RelayCommand(DuplicatePack, () => CanPack);
        DeletePackCommand = new RelayCommand(DeletePack, () => CanPack);
        UploadFontCommand = new RelayCommand(PickUploadFont);
        UploadThemeCommand = new RelayCommand(PickUploadTheme);
        ExportThemeCommand = new RelayCommand(PickExportTheme);
        DeleteThemeCommand = new RelayCommand<string>(DeleteTheme);
        OpenThemeCommand = new RelayCommand<string>(OpenTheme);

        Variant = variant;
        FontIndex = fontIndex;
        SizeIndex = sizeIndex;
        ColourIndex = colourIndex;
        PackIndex = packIndex;
        LanguageIndex = Math.Max(0, Languages.ToList().FindIndex(
            language => string.Equals(language.Code, Localizer.Instance.CurrentLanguage, StringComparison.OrdinalIgnoreCase)));
        CanPack = PackIndex >= 0;
        CanTheme = HasRealTheme();
    }

    public string WslConfigPath { get; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound as an instance property by WPF.")]
    [SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound as an instance property by WPF.")]
    public IReadOnlyList<UiLanguage> Languages => Localizer.Instance.Languages;

    public List<SettingsRow> Fonts { get; private set; }

    public List<SettingsRow> Sizes { get; private set; }

    public List<SettingsRow> Colours { get; private set; }

    public List<SettingsPackRow> Packs { get; private set; }

    public SettingsDto? Result { get; private set; }

    public IRelayCommand CloseCommand { get; }

    public IRelayCommand EditRootCommand { get; }

    public IRelayCommand WslConfigCommand { get; }

    public IRelayCommand OpenPackCommand { get; }

    public IRelayCommand DuplicatePackCommand { get; }

    public IRelayCommand DeletePackCommand { get; }

    public IRelayCommand UploadFontCommand { get; }

    public IRelayCommand UploadThemeCommand { get; }

    public IRelayCommand ExportThemeCommand { get; }

    public IRelayCommand<string> DeleteThemeCommand { get; }

    public IRelayCommand<string> OpenThemeCommand { get; }

    public string SelectedFont => Fonts.Count == 0 ? string.Empty : Fonts[Math.Clamp(FontIndex, 0, Fonts.Count - 1)].Name;

    public int SelectedSize =>
        Sizes.Count == 0 ? 15 : int.Parse(Sizes[Math.Clamp(SizeIndex, 0, Sizes.Count - 1)].Name, System.Globalization.CultureInfo.InvariantCulture);

    public string SelectedColour => Colours.Count == 0 ? string.Empty : Colours[Math.Clamp(ColourIndex, 0, Colours.Count - 1)].Name;

    partial void OnFontIndexChanged(int value)
    {
        Preview();
    }

    partial void OnSizeIndexChanged(int value)
    {
        Preview();
    }

    partial void OnColourIndexChanged(int value)
    {
        CanTheme = HasRealTheme();
        Preview();
    }

    partial void OnVariantChanged(string value)
    {
        Preview();
    }

    partial void OnPackIndexChanged(int value)
    {
        CanPack = value >= 0;
    }

    partial void OnLanguageIndexChanged(int value)
    {
        if (value >= 0 && value < Languages.Count)
        {
            Localizer.Instance.SetLanguage(Languages[value].Code);
        }
    }

    public bool HasRealTheme()
    {
        return SelectedColour.Length > 0;
    }

    public void Preview()
    {
        _ui.Preview(new UiThemeSettings
        {
            FontFamily = SelectedFont,
            FontSize = SelectedSize,
            ColourSet = SelectedColour,
            ColourVariant = Variant,
        });
    }

    public bool HasTheme(string name)
    {
        return Colours.Exists(row => row.Name == name);
    }

    private void PickUploadFont()
    {
        string? source = _dialogs.PickOpenFile(
            Localizer.Instance["FileDialogFont"],
            Localizer.Instance["FileFilterFonts"]);

        if (source is not null)
        {
            UploadFont(source);
        }
    }

    public string? UploadFont(string source)
    {
        string? family = _ui.UploadFont(source);

        if (family is null)
        {
            return null;
        }

        List<SettingsRow> fonts = [.. Fonts, new SettingsRow(family, $"{family}{Localizer.Instance["JustUploadedSuffix"]}")];
        Fonts = fonts;
        OnPropertyChanged(nameof(Fonts));
        FontIndex = fonts.Count - 1;

        return family;
    }

    private void PickUploadTheme()
    {
        string? source = _dialogs.PickOpenFile(
            Localizer.Instance["FileDialogTheme"],
            Localizer.Instance["FileFilterThemes"]);

        if (source is not null)
        {
            UploadTheme(source);
        }
    }

    public void UploadTheme(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        string name = Path.GetFileNameWithoutExtension(source);

        if (HasTheme(name) &&
            !_dialogs.Confirm(
                Localizer.Instance["ReplaceThemeTitle"],
                Localizer.Instance.Format("ConfirmReplaceTheme", name)))
        {
            return;
        }

        ThemeError = string.Empty;

        try
        {
            string uploaded = _ui.UploadTheme(source);
            AddColour(uploaded);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ThemeError = Localizer.Instance.Format("ThemeUploadFailed", exception.ToMessage());
        }
    }

    private void PickExportTheme()
    {
        string name = SelectedColour;
        string proposed = _ui.ThemeFileFor(name);
        string? target = _dialogs.PickSaveFile(
            Localizer.Instance.Format("FileDialogSave", proposed), proposed);

        if (target is not null)
        {
            _ui.ExportTheme(_ui.ThemePathFor(name), target);
        }
    }

    public void DeleteTheme(string? name)
    {
        if (string.IsNullOrEmpty(name) ||
            !_dialogs.Confirm(
                Localizer.Instance["DeleteThemeTitle"],
                Localizer.Instance.Format("ConfirmDeleteTheme", name)))
        {
            return;
        }

        try
        {
            _ui.DeleteTheme(name);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        List<SettingsRow> kept = [.. Colours.Where(row => row.Name != name)];
        int next = kept.Count > 0 ? Math.Min(ColourIndex, kept.Count - 1) : 0;

        Colours = kept;
        OnPropertyChanged(nameof(Colours));
        ColourIndex = next;
        OnPropertyChanged(nameof(SelectedColour));
        CanTheme = HasRealTheme();
        Preview();
    }

    public void OpenTheme(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            _ui.OpenTheme(name);
        }
        catch (InvalidOperationException exception)
        {
            ThemeError = Localizer.Instance.Format("FileDidNotOpen", exception.ToMessage());
        }
    }

    public void OpenPack()
    {
        SettingsPackRow? row = SelectedPack();

        if (row is null)
        {
            return;
        }

        try
        {
            _catalogue.OpenPackFolder(row.Name);
        }
        catch (InvalidOperationException exception)
        {
            CatalogueError = Localizer.Instance.Format("FolderDidNotOpen", exception.ToMessage());
        }
    }

    public void DuplicatePack()
    {
        SettingsPackRow? row = SelectedPack();

        if (row is null)
        {
            return;
        }

        (string Name, string Description)? answer = _dialogs.ShowPackDuplicate(
            row.Name,
            row.Description,
            [.. Packs.Select(pack => pack.Name)]);

        if (answer is not { } duplicate)
        {
            return;
        }

        try
        {
            _catalogue.CreatePack(new CreatePackDto
            {
                SourceName = row.Name,
                NewName = duplicate.Name,
                Description = duplicate.Description,
            });
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            CatalogueError = Localizer.Instance.Format("PackNotDuplicated", exception.ToMessage());

            return;
        }

        RefreshPacks(duplicate.Name);
    }

    public void DeletePack()
    {
        SettingsPackRow? row = SelectedPack();

        if (row is null)
        {
            return;
        }

        string question = Localizer.Instance.Format("ConfirmRemovePack", row.Name);
        string danger = string.Empty;

        if (row.Dependents.Count > 0)
        {
            question += "\n\n" + Localizer.Instance["PackDependentsIntro"];
            question += "\n" + string.Join(
                "\n",
                row.Dependents.Select(dependent => $"- {dependent}"));
            danger = Localizer.Instance["PackDependentsDanger"];
        }

        if (!_dialogs.Confirm(Localizer.Instance["RemovePackTitle"], question, danger))
        {
            return;
        }

        try
        {
            _catalogue.DeletePack(row.Name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            CatalogueError = Localizer.Instance.Format("PackNotRemoved", exception.ToMessage());

            return;
        }

        RefreshPacks(null);
    }

    public void WslConfig()
    {
        WorkflowResult<WslConfigResponseDto> result = _fleetWorkflow.OpenWslConfig();
        ConfigError = result.Status == WorkflowStatus.Success
            ? string.Empty
            : Localizer.Instance.Format("FileDidNotOpen", result.Message);
    }

    public void EditRoot()
    {
        string? chosen = _dialogs.ShowRootMove(Root);

        if (chosen is null)
        {
            return;
        }

        _newRoot = chosen;
        Root = chosen;
        Close();
    }

    public SettingsPackRow? SelectedPack()
    {
        return PackIndex >= 0 && PackIndex < Packs.Count ? Packs[PackIndex] : null;
    }

    private void Close()
    {
        Result = new SettingsDto(
            new SettingsAppearanceDto
            {
                Font = SelectedFont,
                FontSize = SelectedSize,
                ColourScheme = SelectedColour,
                Variant = Variant,
            },
            _newRoot);
        RequestClose();
    }

    private void AddColour(string name)
    {
        SettingsRow? existing = Colours.Find(row => row.Name == name);

        if (existing is not null)
        {
            ColourIndex = Colours.IndexOf(existing);

            return;
        }

        List<SettingsRow> rows = [.. Colours, new SettingsRow(name, $"{name}  (just uploaded)")];
        Colours = rows;
        OnPropertyChanged(nameof(Colours));
        ColourIndex = rows.Count - 1;
        OnPropertyChanged(nameof(SelectedColour));
        CanTheme = HasRealTheme();
    }

    private void RefreshPacks(string? select)
    {
        IReadOnlyList<WslPackDto> refreshed;

        try
        {
            refreshed = _catalogue.GetAllPacks();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Packs = PackRowsOf(refreshed);
        OnPropertyChanged(nameof(Packs));
        PackIndex = Packs.Count > 0 ? 0 : -1;

        if (!string.IsNullOrEmpty(select))
        {
            int index = Packs.FindIndex(row => row.Name == select);

            if (index >= 0)
            {
                PackIndex = index;
            }
        }

        CanPack = PackIndex >= 0;
    }

    private static List<SettingsPackRow> PackRowsOf(IReadOnlyList<WslPackDto> packs)
    {
        return
        [
            .. packs
                .OrderBy(pack => pack.Name, StringComparer.Ordinal)
                .Select(pack => new SettingsPackRow(
                    pack.Name ?? string.Empty,
                    pack.Family ?? string.Empty,
                    pack.Description ?? string.Empty,
                    $"{pack.Name} ({pack.Family})",
                    [.. pack.Dependents])),
        ];
    }
}
