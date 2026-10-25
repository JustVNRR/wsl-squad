using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Gui.Adapters;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class MainWindowViewModel : BaseViewModel
{
    private readonly IInstanceService _instance;
    private readonly IFleetService _fleet;
    private readonly IUiThemeService _ui;
    private readonly IUiLanguageService _languages;
    private readonly IPackCatalogService _catalogue;
    private readonly IDialogService _dialogs;
    private readonly InstanceWorkflow _workflow;
    private readonly FleetWorkflow _fleetWorkflow;
    private readonly ILoggerService _logger;
    private bool InFlight { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RootLine))]
    public partial string Root { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Free))]
    public partial bool Busy { get; set; }

    [ObservableProperty]
    public partial string ColourSet { get; set; }

    [ObservableProperty]
    public partial string Variant { get; set; }

    public MainWindowViewModel(
        IInstanceService instance,
        IFleetService fleet,
        IUiThemeService ui,
        IUiLanguageService languages,
        IPackCatalogService catalogue,
        IDialogService dialogs,
        ILoggerService logger)
    {
        _instance = instance;
        _fleet = fleet;
        _ui = ui;
        _languages = languages;
        _catalogue = catalogue;
        _dialogs = dialogs;
        DialogInstanceInteraction interaction = new(dialogs);
        _workflow = new InstanceWorkflow(
            instance,
            interaction,
            interaction,
            message => SetBusy(true, message));
        _fleetWorkflow = new FleetWorkflow(fleet, executionStarted: message => SetBusy(true, message));
        _logger = logger;
        Root = string.Empty;
        Localizer.Instance.PropertyChanged += OnLocalizerChanged;

        UiThemeSettings theme = ui.GetTheme();
        ColourSet = theme.ColourSet;
        Variant = string.IsNullOrEmpty(theme.ColourVariant) ? "Dark" : theme.ColourVariant;
    }

    public event EventHandler? LookChanged;

    public ObservableCollection<InstanceRowViewModel> Rows { get; } = [];

    public bool Free => !Busy;

    public string RootLine => Localizer.Instance.Format("RootDisplay", Root);

    private bool IsLocked => Busy || InFlight;

    public string ThemeGlyph => Variant == "Light" ? "\uF186" : "\uF185";

    public string ThemeTooltip => Variant == "Light"
        ? Localizer.Instance["ThemeTooltipDark"]
        : Localizer.Instance["ThemeTooltipLight"];

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

        SetBusy(true, Localizer.Instance["StatusReadingFleet"]);

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

        await RunWorkflowAsync(
            Localizer.Instance["OperationBuild"],
            request.Name ?? string.Empty,
            () => _workflow.BuildAsync(request),
            reload: true,
            successMessage: DescribeBuild);
    }

    [RelayCommand]
    private async Task SettingsAsync()
    {
        if (IsLocked)
        {
            return;
        }

        SettingsViewModel viewModel = new(_ui, _languages, _catalogue, _fleet, _dialogs);
        SettingsDto? outcome = _dialogs.ShowSettings(viewModel);

        if (outcome is null)
        {
            return;
        }

        SettingsAppearanceDto appearance = outcome.Appearance;
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
            SetStatus(
                Localizer.Instance.Format("StatusSettingsError", fontFamily, colourSet, exception.ToMessage()),
                true);

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
                Localizer.Instance["OperationMigrate"],
                outcome.NewRoot,
                () => _fleetWorkflow.MigrateRootAsync(outcome.NewRoot),
                reload: true);

            return;
        }

        await LoadAsync();
        SetStatus(
            Localizer.Instance.Format(
                "StatusWindowFace",
                fontFamily,
                fontSize,
                ColourSet.Length > 0 ? ColourSet : Localizer.Instance["DefaultOption"]),
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
            SetStatus(
                Localizer.Instance.Format(
                    "StatusThemeError",
                    Localizer.Instance[other == "Light" ? "ThemeLight" : "ThemeDark"].ToLowerInvariant(),
                    exception.ToMessage()),
                true);

            return;
        }

        Variant = other;
        string setName = ColourSet.Length > 0 ? ColourSet : Localizer.Instance["DefaultOption"];
        SetStatus(
            Localizer.Instance.Format("StatusWindowWears", setName, other.ToLowerInvariant()),
            false);
    }

    private async Task OpenAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationOpen"], name, () => _workflow.OpenAsync(name), reload: false);
    }

    private async Task RestartAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationRestart"], name, () => _workflow.RestartAsync(name), reload: false);
    }

    private async Task EditAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationManagePacks"], name, () => _workflow.ManagePacksAsync(name), reload: false);
    }

    private async Task AppearanceAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationSetAppearance"], name, () => _workflow.AppearanceAsync(name), reload: false);
    }

    private async Task ArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationArchive"], name, () => _workflow.ArchiveAsync(name), reload: true);
    }

    private async Task CompactAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationCompact"], name, () => _workflow.CompactAsync(name), reload: true);
    }

    private async Task RemoveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationRemove"], name, () => _workflow.RemoveAsync(name), reload: true);
    }

    private async Task DockerAsync(string? name, bool register)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (register)
        {
            await RunWorkflowAsync(
                Localizer.Instance["OperationRegisterDocker"],
                name,
                () => _workflow.RegisterDockerAsync(name),
                reload: true);

            return;
        }

        await RunWorkflowAsync(
            Localizer.Instance["OperationUnregisterDocker"],
            name,
            () => _workflow.UnregisterDockerAsync(name),
            reload: true);
    }

    private async Task RestoreAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationRestore"], name, () => _workflow.RestoreAsync(name), reload: true);
    }

    private async Task DuplicateAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationDuplicate"], name, () => _workflow.DuplicateAsync(name), reload: true);
    }

    private async Task DeleteArchiveAsync(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        await RunWorkflowAsync(Localizer.Instance["OperationDeleteArchive"], name, () => _workflow.DeleteArchiveAsync(name), reload: true);
    }

    private async Task RunWorkflowAsync<T>(
        string operation,
        string name,
        Func<Task<WorkflowResult<T>>> run,
        bool reload,
        Func<T, string>? successMessage = null)
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

                    string successText = successMessage is not null && result.Response is not null
                        ? successMessage(result.Response)
                        : Localizer.Instance.Format("StatusOperation", operation, result.Subject);
                    SetStatus(successText, false);

                    break;
                case WorkflowStatus.Cancelled:
                    SetStatus(Localizer.Instance.Format("StatusOperationCancelled", operation), false);

                    break;
                case WorkflowStatus.NoTarget:
                    SetStatus(
                        Localizer.Instance.Format("StatusOperation", operation, result.Message),
                        true);

                    break;
                case WorkflowStatus.Failed:
                    string detail = (result.Subject, result.Message) switch
                    {
                        ({ Length: > 0 } subject, { Length: > 0 } message) =>
                            Localizer.Instance.Format("StatusSubjectDetail", subject, message),
                        ({ Length: > 0 } subject, _) => subject,
                        (_, { Length: > 0 } message) => message,
                        _ => Localizer.Instance["StatusFailed"],
                    };
                    SetStatus(Localizer.Instance.Format("StatusOperation", operation, detail), true);

                    break;
                default:
                    break;
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SetStatus(
                Localizer.Instance.Format(
                    "StatusOperationError", operation, name, exception.ToMessage()),
                true);
        }
        finally
        {
            InFlight = false;
            SetBusy(false, null);
        }
    }

    private static string DescribeBuild(BuildResponseDto response)
    {
        string summary = Localizer.Instance.Format("StatusBuilt", response.InstanceName);
        string[] notices = [.. response.Notices
            .Select(notice => notice.ToMessage().ReplaceLineEndings(" "))
            .Where(message => message.Length > 0)];

        return notices.Length == 0 ? summary : $"{summary} {string.Join(" ", notices)}";
    }

    private async Task LoadAsync(bool announce = true)
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

            if (announce)
            {
                SetStatus(
                    Rows.Count == 0
                        ? Localizer.Instance["StatusNoInstances"]
                        : Localizer.Instance.Format("StatusRowsLoaded", Rows.Count),
                    false);
            }
        }
        catch (InvalidOperationException exception)
        {
            SetStatus(
                Localizer.Instance.Format("StatusReadingError", exception.ToMessage()),
                true);
        }
    }

    private void OnLocalizerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Item[]")
        {
            return;
        }

        OnPropertyChanged(nameof(ThemeTooltip));
        OnPropertyChanged(nameof(RootLine));
        _ = LoadAsync(announce: false);
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
