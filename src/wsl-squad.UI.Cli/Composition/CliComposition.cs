using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Presenters;
using WslSquad.Presentation.Cli.Presenters.Fleet;
using WslSquad.Presentation.Cli.Presenters.Instance;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli.Interaction;
using WslSquad.UI.Cli.Views;

namespace WslSquad.UI.Cli.Composition;

public sealed class CliComposition
{
    private readonly ConsoleTheme _theme = new();
    private readonly ConsoleHost _host;
    private readonly BuildPresenter _build;
    private readonly OpenPresenter _open;
    private readonly RestartPresenter _restart;
    private readonly ManagePacksPresenter _managePacks;
    private readonly SetAppearancePresenter _appearance;
    private readonly RegisterDockerPresenter _registerDocker;
    private readonly UnregisterDockerPresenter _unregisterDocker;
    private readonly RemovePresenter _remove;
    private readonly ArchivePresenter _archive;
    private readonly RestorePresenter _restore;
    private readonly DuplicatePresenter _duplicate;
    private readonly CompactPresenter _compact;
    private readonly MigratePresenter _migrate;
    private readonly WslConfigPresenter _wslConfig;
    private readonly DeleteArchivePresenter _deleteArchive;
    private readonly LanguagePresenter _language;
    private readonly NavigationPresenter _navigation;

    public CliComposition()
    {
        CliServices services = CliServices.Create();
        IInstanceService instance = services.Instance;
        IFleetService fleet = services.Fleet;
        IPackCatalogService catalogue = services.Catalogue;

        _host = new ConsoleHost(_theme);
        CliFleetView fleetView = new(_host);
        CliInstanceView instanceView = new(_host);
        CliBuildView buildView = new(_host);
        CliAppearanceView appearanceView = new(_host);

        _build = new BuildPresenter(instance, buildView);
        _open = new OpenPresenter(instance, instanceView);
        _restart = new RestartPresenter(instance, instanceView);
        _managePacks = new ManagePacksPresenter(instance, instanceView);
        _appearance = new SetAppearancePresenter(instance, instanceView, appearanceView);
        _registerDocker = new RegisterDockerPresenter(instance, instanceView);
        _unregisterDocker = new UnregisterDockerPresenter(instance, instanceView);
        _remove = new RemovePresenter(instance, instanceView);
        _archive = new ArchivePresenter(instance, instanceView);
        _restore = new RestorePresenter(instance, instanceView);
        _duplicate = new DuplicatePresenter(instance, instanceView);
        _compact = new CompactPresenter(instance, instanceView);
        _migrate = new MigratePresenter(fleet, fleetView);
        _wslConfig = new WslConfigPresenter(fleet, fleetView);
        _deleteArchive = new DeleteArchivePresenter(instance, instanceView);

        PresenterAction shell = new("shell", "CliActionShell", name => _open.Run(name), "CliCommandShell");
        PresenterAction restart = new("restart", "CliActionRestart", name => _restart.Run(name), "CliCommandRestart");
        PresenterAction managePacks = new("manage_packs", "CliActionManagePacks", name => _managePacks.Run(name), "CliCommandManagePacks");
        PresenterAction theme = new("theme", "CliActionTheme", name => _appearance.Run(name), "CliCommandTheme");
        PresenterAction duplicate = new("duplicate", "CliActionDuplicate", name => _duplicate.Run(name), "CliCommandDuplicate");
        PresenterAction archive = new("archive", "CliActionArchive", name => _archive.Run(name), "CliCommandArchive");
        PresenterAction compact = new("compact", "CliActionCompact", name => _compact.Run(name), "CliCommandCompact");
        PresenterAction remove = new("unregister", "CliActionRemove", name => _remove.Run(name), "CliCommandRemove");
        PresenterAction register = new("register_docker", "CliActionRegisterDocker", name => _registerDocker.Run(name), "CliCommandRegisterDocker");
        PresenterAction unregister = new("unregister_docker", "CliActionUnregisterDocker", name => _unregisterDocker.Run(name), "CliCommandUnregisterDocker");
        PresenterAction restore = new("restore", "CliActionRestore", name => _restore.Run(name), "CliCommandRestore");
        PresenterAction deleteArchive = new("delete_archive", "CliActionDeleteArchive", name => _deleteArchive.Run(name), "CliCommandDeleteArchive");
        _language = new LanguagePresenter(fleetView);
        PresenterAction languageAction = new("language", "CliActionLanguage", _ => _language.Run(), "CliCommandLanguage");
        PresenterAction migrate = new("migrate", "CliActionMigrate", _ => _migrate.Run(null), "CliCommandMigrate");
        PresenterAction wslConfig = new("wslconfig", "CliActionWslConfig", _ => _wslConfig.Run(), "CliCommandWslConfig");
        PresenterAction packs = new("available_packs", "CliActionPacks", _ => WorkflowResults.Cancelled("Settings"), "CliCommandPacks");

        Dictionary<WslCommandKind, PresenterAction> actions = new()
        {
            [WslCommandKind.Shell] = shell,
            [WslCommandKind.Restart] = restart,
            [WslCommandKind.ManagePacks] = managePacks,
            [WslCommandKind.Appearance] = theme,
            [WslCommandKind.Duplicate] = duplicate,
            [WslCommandKind.Archive] = archive,
            [WslCommandKind.Compact] = compact,
            [WslCommandKind.RegisterDocker] = register,
            [WslCommandKind.UnregisterDocker] = unregister,
            [WslCommandKind.Remove] = remove,
            [WslCommandKind.Restore] = restore,
            [WslCommandKind.DeleteArchive] = deleteArchive,
        };

        BrowsePresenter browse = new(instance, fleetView, actions);

        SettingsPresenter settings = new(catalogue, fleetView, [languageAction, migrate, wslConfig, packs]);
        _navigation = new NavigationPresenter(browse, settings, fleetView);
    }

    public int Run(string? command, string? name)
    {
        try
        {
            if (!string.IsNullOrEmpty(command))
            {
                WorkflowStatus status = RunGesture(command, name).Status;

                return status is WorkflowStatus.Success or WorkflowStatus.Cancelled ? 0 : 1;
            }

            return RunMenu();
        }
        catch (ConsoleQuitException)
        {
            _host.Line(string.Empty);
            _host.Line(Localizer.Instance["CliBye"], ConsoleTone.Muted);

            return 130;
        }
    }

    private int RunMenu()
    {
        bool alternate = ConsoleMode.EnterAlternateScreen();

        try
        {
            while (true)
            {
                ConsoleMenu menu = new(Localizer.Instance["CliMenuTitle"], _host);
                menu.Add("build", Localizer.Instance["CliCommandBuild"], Localizer.Instance["CliMenuBuild"]);
                menu.Add("settings", Localizer.Instance["CliCommandSettings"], Localizer.Instance["CliMenuSettings"]);
                menu.Add("instances", Localizer.Instance["CliCommandInstances"], Localizer.Instance["CliMenuInstances"]);
                menu.Add("archives", Localizer.Instance["CliCommandArchives"], Localizer.Instance["CliMenuArchives"]);
                menu.Add("exit", Localizer.Instance["CliCommandExit"], Localizer.Instance["CliMenuExit"]);
                _host.Clear();
                IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

                if (picked is null || picked[0].Key == "exit")
                {
                    return 0;
                }

                _host.Clear();

                IWorkflowResult result;

                try
                {
                    result = RunGesture(picked[0].Key, string.Empty);
                }
                catch (ConsoleQuitException)
                {
                    return 130;
                }

                if (result.Status == WorkflowStatus.Cancelled)
                {
                    continue;
                }

                _host.Line(string.Empty);
                _host.Line(Localizer.Instance["CliPressEnterMenu"], ConsoleTone.Muted);
                _host.ReadLine(string.Empty);
            }
        }
        finally
        {
            if (alternate)
            {
                ConsoleMode.LeaveAlternateScreen();
            }
        }
    }

    private IWorkflowResult RunGesture(string command, string? name)
    {
        string target = name ?? string.Empty;

        return command switch
        {
            "build" => _build.Run(),
            "language" => _language.Run(),
            "settings" => _navigation.Run("settings"),
            "instances" => _navigation.Run("instances"),
            "archives" => _navigation.Run("archives"),
            "restart" => _restart.Run(target),
            "shell" => _open.Run(target),
            "manage_packs" => _managePacks.Run(target),
            "theme" => _appearance.Run(target),
            "register_docker" => _registerDocker.Run(target),
            "unregister_docker" => _unregisterDocker.Run(target),
            "unregister" => _remove.Run(target),
            "archive" => _archive.Run(target),
            "restore" => _restore.Run(target),
            "duplicate" => _duplicate.Run(target),
            "compact" => _compact.Run(target),
            "migrate" => _migrate.Run(string.IsNullOrEmpty(target) ? null : target),
            "wslconfig" => _wslConfig.Run(),
            "delete_archive" => _deleteArchive.Run(target),
            _ => Unknown(command),
        };
    }

    private WorkflowOutcome Unknown(string command)
    {
        _host.Line(string.Empty);
        _host.Line(Localizer.Instance.Format("CliUnknownCommand", command), ConsoleTone.Error);
        _host.Line(Localizer.Instance["CliCommandsHint1"], ConsoleTone.Hint);
        _host.Line(Localizer.Instance["CliCommandsHint2"], ConsoleTone.Hint);
        _host.Line(Localizer.Instance["CliCommandsHint3"], ConsoleTone.Hint);

        return WorkflowResults.Failed(string.Empty, Localizer.Instance.Format("CliUnknownCommand", command));
    }
}
