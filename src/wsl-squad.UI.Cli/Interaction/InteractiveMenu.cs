using Spectre.Console;

using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Formatting;
using wsl_squad.UI.Cli.Composition;

namespace wsl_squad.UI.Cli.Interaction;

/// <summary>Interactive menu for fleet, instance and archive operations.</summary>
public sealed class InteractiveMenu
{
    private const string _back = "Back";

    private readonly IAnsiConsole _console;

    public InteractiveMenu(IAnsiConsole console)
    {
        _console = console;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? choice = Select(
                    "WSL Squad",
                    ["Build", "Instances", "Archives", "Settings", "Exit"]);

                if (choice is null or "Exit")
                {
                    return 0;
                }

                await DispatchAsync(choice, cancellationToken).ConfigureAwait(false);
                Pause();
            }
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
    }

    private async Task DispatchAsync(string choice, CancellationToken cancellationToken)
    {
        try
        {
            if (choice == "Build")
            {
                await BuildAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (choice == "Instances")
            {
                await InstancesAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (choice == "Archives")
            {
                await ArchivesAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (choice == "Settings")
            {
                ShowSettings();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _console.WriteLine($"error: {exception.Message}");
        }
    }

    private async Task BuildAsync(CancellationToken cancellationToken)
    {
        string name = Text("Instance name");

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string source = Select("Source", ["Recipe", "Dockerfile", "Image", "Registry"]) ?? "Recipe";

        BuildSourceKind kind = source switch
        {
            "Dockerfile" => BuildSourceKind.Dockerfile,
            "Image" => BuildSourceKind.Image,
            "Registry" => BuildSourceKind.Registry,
            _ => BuildSourceKind.Recipe,
        };

        string prompt = "Registry reference";

        if (kind == BuildSourceKind.Recipe)
        {
            prompt = "Recipe name (catalog)";
        }
        else if (kind == BuildSourceKind.Dockerfile)
        {
            prompt = "Dockerfile path";
        }
        else if (kind == BuildSourceKind.Image)
        {
            prompt = "Image archive path";
        }

        string sourcePath = Text(prompt);

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        string user = Text("User (empty = root)");
        string onboarding = Text("Onboarding script (optional)");
        string packs = Text("Packs, comma separated (optional)");
        bool keepImage = Confirm("Keep the built image?", defaultValue: false);
        bool registerDocker = Confirm("Register with Docker Desktop?", defaultValue: false);

        BuildRequest request = new()
        {
            Name = name,
            User = string.IsNullOrWhiteSpace(user) ? null : user,
            SourceKind = kind,
            SourcePath = sourcePath,
            OnboardingPath = string.IsNullOrWhiteSpace(onboarding) ? null : onboarding,
            Packs = Split(packs),
            KeepImage = keepImage,
            RegisterDocker = registerDocker,
        };

        OperationResult<BuildSummary> result = await CliComposition
            .BuildInstanceAsync(request, cancellationToken)
            .ConfigureAwait(false);

        Show(result);

        if (result.IsSuccess)
        {
            _console.WriteLine($"Built '{result.Value.Name}' at {result.Value.InstallPath}.");
        }
    }

    private async Task InstancesAsync(CancellationToken cancellationToken)
    {
        OperationResult<FleetSnapshotDto> snapshot = await CliComposition
            .FleetSnapshotAsync(includeArchives: false, cancellationToken)
            .ConfigureAwait(false);

        if (!snapshot.IsSuccess)
        {
            ShowError(snapshot.Error!);
            return;
        }

        if (snapshot.Value.Instances.Count == 0)
        {
            _console.WriteLine("No live instances.");
            return;
        }

        List<string> choices = [.. snapshot.Value.Instances.Select(instance => instance.Name), _back];
        string? name = Select("Instance", choices);

        if (name is null or _back)
        {
            return;
        }

        while (true)
        {
            string? action = Select(
                $"Instance {name}",
                [
                    "Open", "Restart", "Archive", "Compact", "Duplicate", "Appearance",
                    "Packs", "Docker register", "Docker unregister", "Remove", _back,
                ]);

            if (action is null or _back)
            {
                return;
            }

            await RunInstanceActionAsync(action, name, cancellationToken).ConfigureAwait(false);

            if (action == "Remove")
            {
                return;
            }

            Pause();
        }
    }

    private async Task RunInstanceActionAsync(
        string action,
        string name,
        CancellationToken cancellationToken)
    {
        if (action == "Open")
        {
            Show(CliComposition.Open(name));
        }
        else if (action == "Restart")
        {
            Show(await CliComposition.RestartAsync(name, cancellationToken).ConfigureAwait(false));
        }
        else if (action == "Archive")
        {
            await ArchiveAsync(name, cancellationToken).ConfigureAwait(false);
        }
        else if (action == "Compact")
        {
            bool archiveFirst = Confirm("Archive before compacting?", defaultValue: false);
            OperationResult<CompactOutcome> compacted = await CliComposition
                .CompactAsync(new CompactRequest(name, archiveFirst), cancellationToken)
                .ConfigureAwait(false);
            Show(compacted);

            if (compacted.IsSuccess)
            {
                _console.WriteLine(
                    $"Compacted: {SizeFormatter.Format(compacted.Value.SizeBefore)} -> " +
                    $"{SizeFormatter.Format(compacted.Value.SizeAfter)} " +
                    $"(gained {SizeFormatter.Format(compacted.Value.Gained)}).");
            }
        }
        else if (action == "Duplicate")
        {
            string target = Text("New instance name");

            if (!string.IsNullOrWhiteSpace(target))
            {
                Show(await CliComposition
                    .DuplicateAsync(new DuplicateRequest(name, target), cancellationToken)
                    .ConfigureAwait(false));
            }
        }
        else if (action == "Appearance")
        {
            Appearance(name);
        }
        else if (action == "Packs")
        {
            await PacksAsync(name, cancellationToken).ConfigureAwait(false);
        }
        else if (action == "Docker register")
        {
            Show(await CliComposition.DockerSetAsync(name, integrated: true, restart: true, cancellationToken)
                .ConfigureAwait(false));
        }
        else if (action == "Docker unregister")
        {
            Show(await CliComposition.DockerSetAsync(name, integrated: false, restart: true, cancellationToken)
                .ConfigureAwait(false));
        }
        else if (action == "Remove")
        {
            await RemoveAsync(name, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ArchiveAsync(string name, CancellationToken cancellationToken)
    {
        string format = Select("Archive format", ["tar.gz", "tar"]) ?? ArchiveFormat.Default;
        OperationResult<ArchiveOutcome> archived = await CliComposition
            .ArchiveAsync(
                new ArchiveRequest(name, null, format, Replace: false, StopIfRunning: true),
                cancellationToken)
            .ConfigureAwait(false);

        if (!archived.IsSuccess &&
            archived.Error?.Code == ErrorCode.AlreadyExists &&
            Confirm("Archive already exists. Replace it?", defaultValue: false))
        {
            archived = await CliComposition
                .ArchiveAsync(
                    new ArchiveRequest(name, null, format, Replace: true, StopIfRunning: true),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        Show(archived);

        if (archived.IsSuccess)
        {
            _console.WriteLine($"Archive written to {archived.Value.ArchiveDirectory}.");
        }
    }

    private void Appearance(string name)
    {
        OperationResult<AppearanceOutcome> current = CliComposition.AppearanceGet(name);
        Show(current);

        if (!current.IsSuccess)
        {
            return;
        }

        string font = Text($"Font (current: {current.Value.Font ?? "-"})");
        string scheme = Text($"Colour scheme (current: {current.Value.ColorScheme ?? "-"})");
        string tabTitle = Text($"Tab title (current: {current.Value.TabTitle ?? "-"})");
        string iconText = Text($"Icon text (current: {current.Value.Icon?.Text ?? "-"})");
        string iconImage = Text($"Icon image (current: {current.Value.Icon?.Image ?? "-"})");

        AppearanceRequest request = new(
            string.IsNullOrWhiteSpace(font) ? null : font,
            string.IsNullOrWhiteSpace(scheme) ? null : scheme,
            string.IsNullOrWhiteSpace(tabTitle) ? null : tabTitle,
            string.IsNullOrWhiteSpace(iconText) ? null : iconText,
            IconImage: string.IsNullOrWhiteSpace(iconImage) ? null : iconImage);

        if (request == new AppearanceRequest())
        {
            return;
        }

        Show(CliComposition.AppearanceSet(name, request));
    }

    private async Task PacksAsync(string name, CancellationToken cancellationToken)
    {
        OperationResult<PackStatus> status = await CliComposition
            .PackStatusAsync(name, cancellationToken)
            .ConfigureAwait(false);

        Show(status);

        if (!status.IsSuccess)
        {
            return;
        }

        _console.WriteLine($"Installed: {string.Join(", ", status.Value.Installed)}");
        _console.WriteLine(
            $"Available: {string.Join(", ", status.Value.Candidates.Select(pack => pack.Name))}");

        string add = Text("Packs to add, comma separated (optional)");
        string remove = Text("Packs to remove, comma separated (optional)");

        if (string.IsNullOrWhiteSpace(add) && string.IsNullOrWhiteSpace(remove))
        {
            return;
        }

        Show(await CliComposition
            .PackManageAsync(name, Split(add), Split(remove), cancellationToken)
            .ConfigureAwait(false));
    }

    private async Task RemoveAsync(string name, CancellationToken cancellationToken)
    {
        string confirmation = Text($"Type '{name}' to confirm removal");

        if (!string.Equals(confirmation, name, StringComparison.Ordinal))
        {
            _console.WriteLine("Removal cancelled.");
            return;
        }

        bool archiveFirst = Confirm("Archive before removing?", defaultValue: true);
        Show(await CliComposition
            .RemoveAsync(new RemoveRequest(name, archiveFirst), cancellationToken)
            .ConfigureAwait(false));
    }

    private async Task ArchivesAsync(CancellationToken cancellationToken)
    {
        OperationResult<ArchiveListing> listing = CliComposition.ListArchives();

        if (!listing.IsSuccess)
        {
            ShowError(listing.Error!);
            return;
        }

        if (listing.Value.Archives.Count == 0)
        {
            _console.WriteLine("No archives.");
            return;
        }

        List<string> choices = [.. listing.Value.Archives.Select(archive => archive.Name), _back];
        string? name = Select("Archive", choices);

        if (name is null or _back)
        {
            return;
        }

        string? action = Select($"Archive {name}", ["Restore", _back]);

        if (action != "Restore")
        {
            return;
        }

        string target = Text("Restored instance name");
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        Show(await CliComposition
            .RestoreAsync(new RestoreRequest(name, target), cancellationToken)
            .ConfigureAwait(false));
    }

    private void ShowSettings()
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            ShowError(root.Error!);
            return;
        }

        OperationResult<FleetConfig?> config = context.ConfigStore.Load();
        string catalogRoot = config.IsSuccess
            ? config.Value?.CatalogRoot ?? Path.Combine(
                AppPaths.GetConfigDirectory(context.Platform),
                "catalog")
            : "-";

        _console.WriteLine($"Fleet root: {root.Value}");
        _console.WriteLine($"Configuration: {context.ConfigStore.ConfigPath}");
        _console.WriteLine($"Catalog: {catalogRoot}");

        if (Confirm("Create or show .wslconfig?", defaultValue: false))
        {
            Show(CliComposition.EnsureWslConfig());
        }
    }

    private string? Select(string title, IReadOnlyList<string> choices)
    {
        SelectionPrompt<string> prompt = new SelectionPrompt<string>()
            .Title(title)
            .AddChoices(choices);

        return _console.Prompt(prompt);
    }

    private string Text(string title)
    {
        return _console.Prompt(new TextPrompt<string>(title).AllowEmpty());
    }

    private bool Confirm(string title, bool defaultValue)
    {
        return _console.Confirm(title, defaultValue);
    }

    private void Pause()
    {
        _console.WriteLine(string.Empty);
        _ = _console.Prompt(new TextPrompt<string>("Press Enter to continue...").AllowEmpty());
    }

    private void Show(IOperationResult result)
    {
        if (!result.IsSuccess && result.Error is not null)
        {
            ShowError(result.Error);
        }

        foreach (string warning in result.Warnings)
        {
            _console.WriteLine($"warning: {warning}");
        }
    }

    private void ShowError(OperationError error)
    {
        _console.WriteLine($"error [{error.Code}]: {error.Message}");

        if (!string.IsNullOrWhiteSpace(error.Detail))
        {
            _console.WriteLine(error.Detail);
        }
    }

    private static IReadOnlyList<string> Split(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}
