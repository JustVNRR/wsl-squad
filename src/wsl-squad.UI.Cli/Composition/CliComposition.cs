using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Presenters;
using wsl_squad.Presentation.Cli.Presenters.Fleet;
using wsl_squad.Presentation.Cli.Presenters.Instance;
using wsl_squad.Presentation.Workflows;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Composition;

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
    private readonly NavigationPresenter _navigation;

    public CliComposition()
    {
        CliServices services = CliServices.Create();
        IInstanceService instance = services.Instance;
        IFleetService fleet = services.Fleet;
        IPackCatalogService catalogue = services.Catalogue;

        _host = new ConsoleHost(_theme);
        CliFleetView fleetView = new();
        CliInstanceView instanceView = new();
        CliBuildView buildView = new();
        CliAppearanceView appearanceView = new();

        _build = new BuildPresenter(instance, buildView);
        _open = new OpenPresenter(instance, instanceView);
        _restart = new RestartPresenter(instance, instanceView);
        _managePacks = new ManagePacksPresenter(instance, instanceView);
        _appearance = new SetAppearancePresenter(instance, appearanceView);
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

        PresenterAction shell = new("shell", "open a shell inside an instance", name => _open.Run(name));
        PresenterAction restart = new("restart", "restart an instance", name => _restart.Run(name));
        PresenterAction managePacks = new("manage_packs", "open the pack manager inside an instance", name => _managePacks.Run(name));
        PresenterAction theme = new("theme", "choose the icon, font and colours", name => _appearance.Run(name));
        PresenterAction duplicate = new("duplicate", "copy an instance under another name", name => _duplicate.Run(name));
        PresenterAction archive = new("archive", "write an instance to a named archive", name => _archive.Run(name));
        PresenterAction compact = new("compact", "reclaim the space an instance has freed", name => _compact.Run(name));
        PresenterAction remove = new("unregister", "remove an instance", name => _remove.Run(name));
        PresenterAction register = new("register_docker", "register an instance with Docker Desktop", name => _registerDocker.Run(name));
        PresenterAction unregister = new("unregister_docker", "take an instance out of Docker Desktop", name => _unregisterDocker.Run(name));
        PresenterAction restore = new("restore", "rebuild an instance from an archive", name => _restore.Run(name));
        PresenterAction deleteArchive = new("delete_archive", "delete an archive", name => _deleteArchive.Run(name));
        PresenterAction migrate = new("migrate", "move the fleet to another folder", _ => _migrate.Run(null));
        PresenterAction wslConfig = new("wslconfig", "open the Windows-wide WSL settings", _ => _wslConfig.Run());
        PresenterAction packs = new("availables packs", "edit, duplicate or delete a catalogue pack", _ => WorkflowResults.Cancelled("Settings"));

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

        SettingsPresenter settings = new(catalogue, fleetView, [migrate, wslConfig, packs]);
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
            _host.Line("Bye.", ConsoleTone.Muted);

            return 130;
        }
    }

    private int RunMenu()
    {
        ConsoleMenu menu = new("WSL SQUAD CLI", _host);
        _ = menu.Add("build", "build an instance from the image");
        _ = menu.Add("settings", "the working folder, .wslconfig, and the packs catalog");
        _ = menu.Add("instances", "choose an instance and act on it");
        _ = menu.Add("archives", "choose an archive: restore or delete");
        _ = menu.Add("exit", "leave the CLI");

        bool alternate = ConsoleMode.EnterAlternateScreen();

        try
        {
            while (true)
            {
                _host.Clear();
                IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

                if (picked is null || picked[0].Key == "exit")
                {
                    return 0;
                }

                _host.Clear();

                try
                {
                    _ = RunGesture(picked[0].Key, string.Empty);
                }
                catch (ConsoleQuitException)
                {
                    return 130;
                }

                _host.Line(string.Empty);
                _host.Line("Press Enter to return to the menu.", ConsoleTone.Muted);
                _ = Console.ReadLine();
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
        _host.Line($"Unknown command '{command}'.", ConsoleTone.Error);
        _host.Line("Commands: build, settings, instances, archives, restart, shell, manage_packs, theme,", ConsoleTone.Hint);
        _host.Line("          register_docker, unregister_docker, unregister, archive, restore, duplicate,", ConsoleTone.Hint);
        _host.Line("          compact, migrate, wslconfig, delete_archive.", ConsoleTone.Hint);

        return WorkflowResults.Failed(string.Empty, $"Unknown command '{command}'.");
    }
}
