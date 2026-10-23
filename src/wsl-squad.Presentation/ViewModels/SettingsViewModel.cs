using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.Settings;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.ViewModels;

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
    private int _fontIndex;

    [ObservableProperty]
    private int _sizeIndex;

    [ObservableProperty]
    private int _colourIndex;

    [ObservableProperty]
    private int _packIndex = -1;

    [ObservableProperty]
    private string _root;

    [ObservableProperty]
    private string _variant;

    [ObservableProperty]
    private string _themeError = string.Empty;

    [ObservableProperty]
    private string _catalogueError = string.Empty;

    [ObservableProperty]
    private string _configError = string.Empty;

    [ObservableProperty]
    private bool _canTheme;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenPackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicatePackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePackCommand))]
    private bool _canPack;

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

        UiThemeSettings theme = ui.GetTheme();
        _variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;

        try
        {
            _root = fleet.GetRoot();
        }
        catch (InvalidOperationException)
        {
            _root = string.Empty;
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
        _fontIndex = Math.Max(0, Fonts.FindIndex(row => string.Equals(row.Name, theme.FontFamily, StringComparison.Ordinal)));

        List<int> sizes = [11, 13, 15, 17, 19];

        if (!sizes.Contains(theme.FontSize))
        {
            sizes.Insert(0, theme.FontSize);
        }

        Sizes = [.. sizes.Select(size => new SettingsRow(
            size.ToString(System.Globalization.CultureInfo.InvariantCulture),
            size.ToString(System.Globalization.CultureInfo.InvariantCulture)))];
        _sizeIndex = Math.Max(0, Sizes.FindIndex(row =>
            int.TryParse(row.Name, out int size) && size == theme.FontSize));

        List<string> sets = [];
        if (!themes.Contains(theme.ColourSet, StringComparer.Ordinal))
        {
            sets.Add(theme.ColourSet);
        }

        sets.AddRange(themes);

        Colours = [.. sets.Select(name => new SettingsRow(name, name.Length == 0 ? "Default" : name))];
        _colourIndex = Math.Max(0, Colours.FindIndex(row =>
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
        _packIndex = Packs.Count > 0 ? 0 : -1;
        _canPack = _packIndex >= 0;
        _canTheme = HasRealTheme();

        CloseCommand = new RelayCommand(Close);
        WslConfigCommand = new RelayCommand(WslConfig);
        OpenPackCommand = new RelayCommand(OpenPack, () => CanPack);
        DuplicatePackCommand = new RelayCommand(DuplicatePack, () => CanPack);
        DeletePackCommand = new RelayCommand(DeletePack, () => CanPack);
        UploadThemeCommand = new RelayCommand<string>(UploadTheme);
        DeleteThemeCommand = new RelayCommand<string>(DeleteTheme);
        OpenThemeCommand = new RelayCommand<string>(OpenTheme);
    }

    public string WslConfigPath { get; }

    public List<SettingsRow> Fonts { get; private set; }

    public List<SettingsRow> Sizes { get; private set; }

    public List<SettingsRow> Colours { get; private set; }

    public List<SettingsPackRow> Packs { get; private set; }

    public SettingsDto? Result { get; private set; }

    public IRelayCommand CloseCommand { get; }

    public IRelayCommand WslConfigCommand { get; }

    public IRelayCommand OpenPackCommand { get; }

    public IRelayCommand DuplicatePackCommand { get; }

    public IRelayCommand DeletePackCommand { get; }

    public IRelayCommand<string> UploadThemeCommand { get; }

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

    public string? UploadFont(string source)
    {
        string? family = _ui.UploadFont(source);

        if (family is null)
        {
            return null;
        }

        List<SettingsRow> fonts = [.. Fonts, new SettingsRow(family, $"{family}  (just uploaded)")];
        Fonts = fonts;
        OnPropertyChanged(nameof(Fonts));
        FontIndex = fonts.Count - 1;

        return family;
    }

    public void UploadTheme(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        try
        {
            string name = _ui.UploadTheme(source);
            AddColour(name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            ThemeError = $"The theme was not uploaded: {exception.Message}";
        }
    }

    public void FailTheme(string message)
    {
        ThemeError = message;
    }

    public void ClearThemeError()
    {
        ThemeError = string.Empty;
    }

    public string ThemePathFor(string name)
    {
        return _ui.ThemePathFor(name);
    }

    public string ThemeFileFor(string name)
    {
        return _ui.ThemeFileFor(name);
    }

    public void ExportTheme(string? target, string? source = null)
    {
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        _ui.ExportTheme(source ?? ThemePathFor(SelectedColour), target);
    }

    public void DeleteTheme(string? name)
    {
        if (string.IsNullOrEmpty(name))
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
            ThemeError = $"The file did not open: {exception.Message}";
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
            CatalogueError = $"The folder did not open: {exception.Message}";
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
            _ = _catalogue.CreatePack(new CreatePackDto
            {
                SourceName = row.Name,
                NewName = duplicate.Name,
                Description = duplicate.Description,
            });
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            CatalogueError = $"The pack was not duplicated: {exception.Message}";

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

        string question = $"Remove the pack '{row.Name}'?";
        string danger = string.Empty;

        if (row.Dependents.Count > 0)
        {
            question += "\n\nThese packs depend on it:";
            question += "\n" + string.Join(
                "\n",
                row.Dependents.Select(dependent => $"- {dependent}"));
            danger = "Removing it may break them.";
        }

        if (!_dialogs.Confirm("Remove the pack", question, danger))
        {
            return;
        }

        try
        {
            _catalogue.DeletePack(row.Name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            CatalogueError = $"The pack was not removed: {exception.Message}";

            return;
        }

        RefreshPacks(null);
    }

    public void WslConfig()
    {
        WorkflowResult<WslConfigResponseDto> result = _fleetWorkflow.OpenWslConfig();
        ConfigError = result.Status == WorkflowStatus.Success
            ? string.Empty
            : $"The file did not open: {result.Message}";
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
            new SetAppearanceDto
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
