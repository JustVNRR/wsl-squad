using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class MainWindowViewModel : BaseViewModel
{
    private readonly IInstanceService _instance;
    private readonly IFleetService _fleet;
    private readonly IUiThemeService _ui;
    private readonly IPackCatalogService _catalogue;
    private readonly IDialogService _dialogs;
    private readonly IBuildLauncher _buildLauncher;
    private readonly InstanceWorkflow _workflow;
    private readonly FleetWorkflow _fleetWorkflow;
    private readonly ILoggerService _logger;
    private bool InFlight { get; set; }

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
        DialogInstanceInteraction interaction = new(dialogs);
        _workflow = new InstanceWorkflow(
            instance,
            interaction,
            interaction,
            message => SetBusy(true, message));
        _fleetWorkflow = new FleetWorkflow(fleet, executionStarted: message => SetBusy(true, message));
        _logger = logger;

        UiThemeSettings theme = ui.GetTheme();
        _colourSet = theme.ColourSet;
        _variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;
    }

    public event EventHandler? LookChanged;

    public ObservableCollection<InstanceRowViewModel> Rows { get; } = [];

    public bool Free => !Busy;

    private bool IsLocked => Busy || InFlight;

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
        if (IsLocked)
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
        if (IsLocked || request is null || string.IsNullOrEmpty(request.Name))
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
                await EditAsync(request.Name);
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
        if (IsLocked)
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
        if (IsLocked)
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
            await RunWorkflowAsync(
                "Migrate",
                outcome.NewRoot,
                () => _fleetWorkflow.MigrateRootAsync(outcome.NewRoot),
                reload: true);

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
        if (IsLocked)
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
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Open", name, () => _workflow.OpenAsync(name), reload: false);
    }

    private async Task RestartAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Restart", name, () => _workflow.RestartAsync(name), reload: false);
    }

    private async Task EditAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("ManagePacks", name, () => _workflow.ManagePacksAsync(name), reload: false);
    }

    private async Task AppearanceAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("SetAppearance", name, () => _workflow.AppearanceAsync(name), reload: false);
    }

    private async Task ArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Archive", name, () => _workflow.ArchiveAsync(name), reload: true);
    }

    private async Task CompactAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Compact", name, () => _workflow.CompactAsync(name), reload: true);
    }

    private async Task RemoveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Remove", name, () => _workflow.RemoveAsync(name), reload: true);
    }

    private async Task DockerAsync(string? name, bool register)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        Func<Task<WorkflowResult<bool>>> run = register
            ? () => _workflow.RegisterDockerAsync(name)
            : () => _workflow.UnregisterDockerAsync(name);

        await RunWorkflowAsync(register ? "RegisterDocker" : "UnregisterDocker", name, run, reload: false);
    }

    private async Task RestoreAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Restore", name, () => _workflow.RestoreAsync(name), reload: true);
    }

    private async Task DuplicateAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("Duplicate", name, () => _workflow.DuplicateAsync(name), reload: true);
    }

    private async Task DeleteArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync("DeleteArchive", name, () => _workflow.DeleteArchiveAsync(name), reload: true);
    }

    private async Task RunWorkflowAsync<T>(
        string operation,
        string name,
        Func<Task<WorkflowResult<T>>> run,
        bool reload)
    {
        if (IsLocked)
        {
            return;
        }

        InFlight = true;

        try
        {
            WorkflowResult<T> result = await run();

            switch (result.Status)
            {
                case WorkflowStatus.Success:
                    if (reload)
                    {
                        await LoadAsync();
                    }

                    SetStatus($"{operation} : {result.Subject}", false);

                    break;
                case WorkflowStatus.Cancelled:
                    SetStatus($"{operation} : cancelled", false);

                    break;
                case WorkflowStatus.NoTarget:
                    SetStatus($"{operation} : no instance", true);

                    break;
                case WorkflowStatus.Failed:
                    string detail = string.IsNullOrEmpty(result.Message)
                        ? result.Subject
                        : $"{result.Subject} - {result.Message}";
                    SetStatus($"{operation} : {detail}", true);

                    break;
                default:
                    break;
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"{operation} : {name} - {exception.Message}", true);
        }
        finally
        {
            InFlight = false;
            SetBusy(false, null);
        }
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
