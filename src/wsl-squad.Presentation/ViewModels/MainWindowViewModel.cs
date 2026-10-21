using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.Settings;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class MainWindowViewModel : BaseViewModel
{
    private readonly IInstanceService _instance;
    private readonly IFleetService _fleet;
    private readonly IUiThemeService _ui;
    private readonly IPackCatalogService _catalogue;
    private readonly IDialogService _dialogs;
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private string _root = string.Empty;

    [ObservableProperty]
    private bool _busy;

    [ObservableProperty]
    private bool _free = true;

    [ObservableProperty]
    private string _colourSet;

    [ObservableProperty]
    private string _variant;

    public MainWindowViewModel(
        IInstanceService instance,
        IFleetService fleet,
        IUiThemeService ui,
        IPackCatalogService catalogue,
        IDialogService dialogs,
        ILoggerService logger)
    {
        _instance = instance;
        _fleet = fleet;
        _ui = ui;
        _catalogue = catalogue;
        _dialogs = dialogs;
        _logger = logger;

        UiThemeSettings theme = ui.GetTheme();
        _colourSet = theme.ColourSet;
        _variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;
    }

    public event EventHandler? LookChanged;

    public ObservableCollection<InstanceRowViewModel> Rows { get; } = [];

    public bool CanToggleTheme => true;

    public string ThemeGlyph => Variant == "Light" ? "\uF186" : "\uF185";

    public string ThemeTooltip => Variant == "Light" ? "Switch to dark" : "Switch to light";

    partial void OnVariantChanged(string value)
    {
        OnPropertyChanged(nameof(ThemeGlyph));
        OnPropertyChanged(nameof(ThemeTooltip));
    }

    [RelayCommand]
    private void Refresh()
    {
        Load();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        BuildViewModel viewModel = new(_instance, _dialogs);
        BuildDto? request = _dialogs.ShowBuild(viewModel);

        if (request is null)
        {
            return;
        }

        SetBusy(true, $"Building '{request.Name}' in a terminal...");

        try
        {
            int exitCode = await _dialogs.ShowBuildTerminalAsync(request);
            Load();
            SetStatus(
                exitCode == 0
                    ? $"Created '{request.Name}'."
                    : $"The build of '{request.Name}' ended with code {exitCode}.",
                exitCode != 0);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"Creating '{request.Name}' - {exception.Message}", true);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    [RelayCommand]
    private async Task SettingsAsync()
    {
        SettingsViewModel viewModel = new(_ui, _catalogue, _fleet, _dialogs);
        SettingsOutcome? outcome = _dialogs.ShowSettings(viewModel);

        if (outcome is null)
        {
            return;
        }

        string previousFont = _ui.GetTheme().FontFamily;
        int previousSize = _ui.GetTheme().FontSize;

        try
        {
            _ui.SetFontFamily(outcome.FontFamily);
            _ui.SetFontSize(outcome.FontSize);
            _ui.SetColorTheme(outcome.ColourSet);
            _ui.SetVariant(outcome.Variant);

            _ui.Preview(outcome.FontFamily, outcome.FontSize, outcome.ColourSet, outcome.Variant);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Settings : {outcome.FontFamily}/{outcome.ColourSet} - {exception.Message}", true);

            return;
        }

        if (outcome.ColourSet.Length > 0)
        {
            ColourSet = outcome.ColourSet;
        }

        Variant = outcome.Variant;

        if (previousSize != outcome.FontSize ||
            !string.Equals(previousFont, outcome.FontFamily, StringComparison.Ordinal))
        {
            LookChanged?.Invoke(this, EventArgs.Empty);
        }

        if (outcome.NewRoot is not null)
        {
            await RunAsync(
                $"Moving the fleet to '{outcome.NewRoot}'...",
                () => _fleet.Migrate(outcome.NewRoot),
                $"Fleet moved to '{outcome.NewRoot}'.");

            return;
        }

        Load();
        SetStatus(
            $"Window face: '{outcome.FontFamily}' at {outcome.FontSize} pt, theme '{(ColourSet.Length > 0 ? ColourSet : "default")}'. Popups follow on their next open.",
            false);
    }

    [RelayCommand]
    private void Theme()
    {
        string other = Variant == "Light" ? "Dark" : "Light";

        try
        {
            _ui.SetVariant(other);

            UiThemeSettings look = _ui.GetTheme();
            _ui.Preview(look.FontFamily, look.FontSize, look.ColourSet, other);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Could not keep the {other} version: {exception.Message}", true);

            return;
        }

        Variant = other;
        string setName = ColourSet.Length > 0 ? ColourSet : "default";
        SetStatus($"The window wears '{setName}', {other.ToLowerInvariant()} version.", false);
    }

    [RelayCommand]
    private async Task OpenAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            _ = await Task.Run(() => _instance.Open(name));
            SetStatus($"Open : {name}", false);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Open : {name} - {exception.Message}", true);
        }
    }

    [RelayCommand]
    private async Task RestartAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunAsync(
            $"Restarting '{name}'...",
            () => _instance.Restart(name),
            $"Restarted '{name}'.");
    }

    [RelayCommand]
    private void Edit(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            _instance.OpenPackManager(name);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"ManagePacks : {name} - {exception.Message}", true);

            return;
        }

        SetStatus($"Editing '{name}'s packs in a terminal.", false);
    }

    [RelayCommand]
    private async Task AppearanceAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        GetAppearanceResponseDto appearance;
        IReadOnlyList<string> fonts;
        IReadOnlyList<WslColorSchemeDto> schemes;
        IReadOnlyList<WslIconPairDto> pairs;

        try
        {
            appearance = _instance.GetAppearance(name);
            fonts = _instance.GetFonts();
            schemes = _instance.GetColorSchemes();
            pairs = _instance.GetIconPairs();
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"SetAppearance : {name} - {exception.Message}", true);

            return;
        }

        SetAppearanceViewModel viewModel = new(name, appearance, fonts, schemes, pairs);
        SetAppearanceDto? request = _dialogs.ShowSetAppearance(viewModel);

        if (request is null)
        {
            SetStatus("SetAppearance : cancelled", false);

            return;
        }

        await RunAsync(
            $"Applying '{name}'s appearance...",
            () => _instance.SetAppearance(request),
            $"SetAppearance : {name}");
    }

    [RelayCommand]
    private async Task ArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        IReadOnlyList<WslIndexDto> all;

        try
        {
            all = _instance.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Archive : {name} - {exception.Message}", true);

            return;
        }

        ArchiveDto? request = _dialogs.ShowArchive(new ArchiveViewModel(name, all));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Archiving '{name}' as '{name}_archive'...",
            () => _instance.Archive(request),
            $"Archived '{name}'.");
    }

    [RelayCommand]
    private async Task CompactAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        CompactDto? request = _dialogs.ShowCompact(new CompactViewModel(name));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Compacting '{name}'...",
            () => _instance.Compact(request),
            $"Compacted '{name}'.");
    }

    [RelayCommand]
    private async Task RemoveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        RemoveDto? request = _dialogs.ShowRemove(new RemoveViewModel(name));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Removing '{name}'...",
            () => _instance.Remove(request),
            $"Removed '{name}'.");
    }

    [RelayCommand]
    private async Task DockerAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string state = Rows.FirstOrDefault(row => row.Name == name)?.Docker ?? string.Empty;

        if (state is not "yes" and not "no")
        {
            SetStatus($"DockerDesktop : {name} - its Docker Desktop state cannot be read.", true);

            return;
        }

        DockerDialogResult? answer = _dialogs.ShowDockerRegister(new DockerRegisterViewModel(name, state));

        if (answer is null)
        {
            return;
        }

        if (state == "yes")
        {
            await RunAsync(
                $"Taking '{name}' out of Docker Desktop...",
                () => _instance.UnregisterDocker(new UnregisterDockerDto
                {
                    InstanceName = name,
                    Restart = answer.Restart,
                }),
                $"Unregistered '{name}' from Docker Desktop.");
        }
        else
        {
            await RunAsync(
                $"Registering '{name}' with Docker Desktop...",
                () => _instance.RegisterDocker(new RegisterDockerDto
                {
                    InstanceName = name,
                    Restart = answer.Restart,
                }),
                $"Registered '{name}' with Docker Desktop.");
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        RestoreDto? request = _dialogs.ShowRestore(new RestoreViewModel(name));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Restoring '{name}' as '{request.InstanceName}'...",
            () => _instance.Restore(request),
            $"Restored '{name}' as '{request.InstanceName}'.");
    }

    [RelayCommand]
    private async Task DuplicateAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        DuplicateDto? request = _dialogs.ShowDuplicate(new DuplicateViewModel(name));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Duplicating '{name}' as '{request.CopyName}'...",
            () => _instance.Duplicate(request),
            $"Duplicated '{name}' as '{request.CopyName}'.");
    }

    [RelayCommand]
    private async Task DeleteArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string? named = _dialogs.ShowDeleteArchive(new DeleteArchiveViewModel(name));

        if (named is null)
        {
            return;
        }

        await RunAsync(
            $"Deleting the archive '{named}'...",
            () => _instance.DeleteArchive(named),
            $"Deleted the archive '{named}'.");
    }

    private void Load()
    {
        try
        {
            Root = _fleet.GetRoot();
            Rows.Clear();

            foreach (WslIndexDto row in _instance.GetAll()
                         .Where(row => row is not null)
                         .OrderBy(row => row.Name, StringComparer.Ordinal))
            {
                Rows.Add(InstanceRowViewModel.From(row));
            }

            SetStatus(
                Rows.Count == 0 ? "No WSL SQUAD instances found." : $"{Rows.Count} row(s) loaded.",
                false);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Error reading instances: {exception.Message}", true);
        }
    }

    private async Task RunAsync(string message, Action action, string success)
    {
        if (Busy)
        {
            return;
        }

        SetBusy(true, message);

        try
        {
            await Task.Run(action);
            Load();
            SetStatus(success, false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"{message} - {exception.Message}", true);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void SetBusy(bool busy, string? message)
    {
        Busy = busy;
        Free = !busy;

        if (!string.IsNullOrEmpty(message))
        {
            SetStatus(message, false);
        }
    }

    private void SetStatus(string message, bool alert)
    {
        LogEntryDto entry = new()
        {
            When = DateTime.Now,
            Level = alert ? "ERROR" : "INFO",
            Message = message,
        };

        _logger.Write(entry);

        if (alert)
        {
            _dialogs.ShowError(entry);
        }
    }
}
