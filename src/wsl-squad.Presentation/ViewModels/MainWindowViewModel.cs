using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;
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
    private readonly IBuildLauncher _buildLauncher;
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private string _root = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Free))]
    private bool _busy;

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
        IBuildLauncher buildLauncher,
        ILoggerService logger)
    {
        _instance = instance;
        _fleet = fleet;
        _ui = ui;
        _catalogue = catalogue;
        _dialogs = dialogs;
        _buildLauncher = buildLauncher;
        _logger = logger;

        UiThemeSettings theme = ui.GetTheme();
        _colourSet = theme.ColourSet;
        _variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;
    }

    public event EventHandler? LookChanged;

    public ObservableCollection<InstanceRowViewModel> Rows { get; } = [];

    public bool Free => !Busy;

    public string ThemeGlyph => Variant == "Light" ? "\uF186" : "\uF185";

    public string ThemeTooltip => Variant == "Light" ? "Switch to dark" : "Switch to light";

    partial void OnVariantChanged(string value)
    {
        OnPropertyChanged(nameof(ThemeGlyph));
        OnPropertyChanged(nameof(ThemeTooltip));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Busy)
        {
            return;
        }

        SetBusy(true, "Reading the fleet...");

        try
        {
            await LoadAsync();
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    [RelayCommand]
    private async Task RunRowAsync(InstanceCommandParameter? request)
    {
        if (Busy || request is null || string.IsNullOrEmpty(request.Name))
        {
            return;
        }

        switch (request.Kind)
        {
            case WslCommandKind.Shell:
                await OpenAsync(request.Name);
                break;
            case WslCommandKind.Restart:
                await RestartAsync(request.Name);
                break;
            case WslCommandKind.ManagePacks:
                Edit(request.Name);
                break;
            case WslCommandKind.Appearance:
                await AppearanceAsync(request.Name);
                break;
            case WslCommandKind.Duplicate:
                await DuplicateAsync(request.Name);
                break;
            case WslCommandKind.Archive:
                await ArchiveAsync(request.Name);
                break;
            case WslCommandKind.Compact:
                await CompactAsync(request.Name);
                break;
            case WslCommandKind.RegisterDocker:
                await DockerAsync(request.Name, register: true);
                break;
            case WslCommandKind.UnregisterDocker:
                await DockerAsync(request.Name, register: false);
                break;
            case WslCommandKind.Remove:
                await RemoveAsync(request.Name);
                break;
            case WslCommandKind.Restore:
                await RestoreAsync(request.Name);
                break;
            case WslCommandKind.DeleteArchive:
                await DeleteArchiveAsync(request.Name);
                break;
            default:
                break;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (Busy)
        {
            return;
        }

        BuildViewModel viewModel = new(_instance, _dialogs);
        BuildDto? request = _dialogs.ShowBuild(viewModel);

        if (request is null)
        {
            return;
        }

        SetBusy(true, $"Building '{request.Name}' in a terminal...");

        try
        {
            int exitCode = await _buildLauncher.RunAsync(request);
            await LoadAsync();
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
        if (Busy)
        {
            return;
        }

        SettingsViewModel viewModel = new(_ui, _catalogue, _fleet, _dialogs);
        SettingsDto? outcome = _dialogs.ShowSettings(viewModel);

        if (outcome is null)
        {
            return;
        }

        SetAppearanceDto appearance = outcome.Appearance;
        string fontFamily = appearance.Font ?? string.Empty;
        int fontSize = appearance.FontSize;
        string colourSet = appearance.ColourScheme ?? string.Empty;
        string variant = appearance.Variant ?? string.Empty;

        string previousFont = _ui.GetTheme().FontFamily;
        int previousSize = _ui.GetTheme().FontSize;

        UiThemeSettings look = new()
        {
            FontFamily = fontFamily,
            FontSize = fontSize,
            ColourSet = colourSet,
            ColourVariant = variant,
        };

        try
        {
            _ui.ApplyTheme(look);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus($"Settings : {fontFamily}/{colourSet} - {exception.Message}", true);

            return;
        }

        if (colourSet.Length > 0)
        {
            ColourSet = colourSet;
        }

        Variant = variant;

        if (previousSize != fontSize ||
            !string.Equals(previousFont, fontFamily, StringComparison.Ordinal))
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

        await LoadAsync();
        SetStatus(
            $"Window face: '{fontFamily}' at {fontSize} pt, theme '{(ColourSet.Length > 0 ? ColourSet : "default")}'. Popups follow on their next open.",
            false);
    }

    [RelayCommand]
    private void Theme()
    {
        if (Busy)
        {
            return;
        }

        string other = Variant == "Light" ? "Dark" : "Light";

        UiThemeSettings current = _ui.GetTheme();
        UiThemeSettings look = new()
        {
            FontFamily = current.FontFamily,
            FontSize = current.FontSize,
            ColourSet = current.ColourSet,
            ColourVariant = other,
        };

        try
        {
            _ui.ApplyTheme(look);
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

        private async Task OpenAsync(string? name)
    {
        if (Busy || string.IsNullOrEmpty(name))
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

        private void Edit(string? name)
    {
        if (Busy || string.IsNullOrEmpty(name))
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

        private async Task ArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        bool replacesArchive = Rows.Any(row =>
            row.IsArchive && row.Name == $"{name}_archive");
        ArchiveDto? request = _dialogs.ShowArchive(new ArchiveViewModel(name, replacesArchive));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Archiving '{name}' as '{name}_archive'...",
            () => _instance.Archive(request),
            $"Archived '{name}'.");
    }

        private async Task CompactAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        bool replacesArchive = Rows.Any(row =>
            row.IsArchive && row.Name == $"{name}_archive");
        CompactDto? request = _dialogs.ShowCompact(new CompactViewModel(name, replacesArchive));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Compacting '{name}'...",
            () => _instance.Compact(request),
            $"Compacted '{name}'.");
    }

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

    private async Task DockerAsync(string? name, bool register)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string state = Rows.FirstOrDefault(row => row.Name == name)?.Docker ?? string.Empty;
        DockerTargetStatus status = DockerRules.Status(state, register);

        if (status == DockerTargetStatus.Unknown)
        {
            SetStatus($"DockerDesktop : {name} - its Docker Desktop state cannot be read.", true);

            return;
        }

        if (status != DockerTargetStatus.Eligible)
        {
            string reason = status == DockerTargetStatus.AlreadyRegistered
                ? "it is already registered"
                : "it is not registered";
            SetStatus($"DockerDesktop : {name} - {reason}.", true);

            return;
        }

        bool? restart = _dialogs.ShowDockerRegister(new DockerRegisterViewModel(name, state));

        if (restart is null)
        {
            return;
        }

        if (register)
        {
            await RunAsync(
                $"Registering '{name}' with Docker Desktop...",
                () => _instance.RegisterDocker(new RegisterDockerDto
                {
                    InstanceName = name,
                    Restart = restart.Value,
                }),
                $"Registered '{name}' with Docker Desktop.");
        }
        else
        {
            await RunAsync(
                $"Taking '{name}' out of Docker Desktop...",
                () => _instance.UnregisterDocker(new UnregisterDockerDto
                {
                    InstanceName = name,
                    Restart = restart.Value,
                }),
                $"Unregistered '{name}' from Docker Desktop.");
        }
    }

        private async Task RestoreAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string[] taken = [.. Rows.Where(row => !row.IsArchive).Select(row => row.Name)];
        RestoreDto? request = _dialogs.ShowRestore(new RestoreViewModel(name, taken));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Restoring '{name}' as '{request.InstanceName}'...",
            () => _instance.Restore(request),
            $"Restored '{name}' as '{request.InstanceName}'.");
    }

        private async Task DuplicateAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string[] taken = [.. Rows.Select(row => row.Name)];
        DuplicateDto? request = _dialogs.ShowDuplicate(new DuplicateViewModel(name, taken));

        if (request is null)
        {
            return;
        }

        await RunAsync(
            $"Duplicating '{name}' as '{request.CopyName}'...",
            () => _instance.Duplicate(request),
            $"Duplicated '{name}' as '{request.CopyName}'.");
    }

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

    private async Task LoadAsync()
    {
        try
        {
            (string root, IReadOnlyList<WslIndexDto> rows) = await Task.Run(
                () => (_fleet.GetRoot(), _instance.GetAll()));

            Root = root;
            Rows.Clear();

            foreach (WslIndexDto row in rows
                         .Where(row => row is not null)
                         .OrderBy(row => row.Name, StringComparer.Ordinal))
            {
                Rows.Add(InstanceRowViewModel.From(row, RunRowCommand));
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
            await LoadAsync();
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
