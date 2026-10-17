using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.UI.Cli.Commands;
using wsl_squad.UI.Cli.Interaction;

CommandApp app = new();
app.Configure(config =>
{
    _ = config.SetApplicationName("wsl-squad");
    _ = config.AddCommand<ListCommand>("list")
        .WithDescription("List managed WSL instances, orphans and archives.");
    _ = config.AddCommand<InitCommand>("init")
        .WithDescription("Create or adopt a fleet root and write config.json.");
    _ = config.AddCommand<BuildCommand>("build")
        .WithDescription("Build a new WSL instance from a Dockerfile, recipe, image or registry reference.");
    _ = config.AddCommand<OpenCommand>("open")
        .WithDescription("Open a shell for an instance.");
    _ = config.AddCommand<OpenCommand>("shell")
        .WithDescription("Open a shell for an instance.");
    _ = config.AddCommand<RestartCommand>("restart")
        .WithDescription("Terminate and reopen an instance.");
    _ = config.AddCommand<DuplicateCommand>("duplicate")
        .WithDescription("Duplicate an instance under another name.");
    _ = config.AddCommand<RestoreCommand>("restore")
        .WithDescription("Restore an instance from an archive.");
    _ = config.AddCommand<RemoveCommand>("remove")
        .WithDescription("Remove an instance, optionally archiving it first.");
    _ = config.AddCommand<CompactCommand>("compact")
        .WithDescription("Compact an instance virtual disk.");
    _ = config.AddBranch("archive", archive => archive
        .AddCommand<ArchiveCreateCommand>("create")
        .WithDescription("Archive an instance."));
    _ = config.AddBranch("archives", archives =>
    {
        _ = archives.AddCommand<ArchivesListCommand>("list")
            .WithDescription("List archives.");
        _ = archives.AddCommand<DeleteArchiveCommand>("delete")
            .WithDescription("Delete an archive.");
    });
    _ = config.AddCommand<AppearanceCommand>("appearance")
        .WithDescription("Show or change an instance's terminal appearance.");
    _ = config.AddCommand<WslConfigCommand>("wslconfig")
        .WithDescription("Create or show the Windows-wide .wslconfig file.");
    _ = config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Check the environment and report problems.");
    _ = config.AddBranch("migrate", migrate =>
    {
        _ = migrate.AddCommand<MigrateLegacyCommand>("legacy")
            .WithDescription("Adopt a legacy .wsl-fleet fleet (use --dry-run first).");
        _ = migrate.AddCommand<MigrateRollbackCommand>("rollback")
            .WithDescription("Roll back a migration from its backup id.");
    });
    _ = config.AddBranch("root", root =>
    {
        _ = root.AddCommand<RootShowCommand>("show")
            .WithDescription("Show the resolved fleet root and configuration paths.");
        _ = root.AddCommand<RootMoveCommand>("move")
            .WithDescription("Move the fleet to another folder (destructive).");
    });
    _ = config.AddBranch("docker", docker =>
    {
        _ = docker.AddCommand<DockerRegisterCommand>("register")
            .WithDescription("Register an instance with Docker Desktop.");
        _ = docker.AddCommand<DockerUnregisterCommand>("unregister")
            .WithDescription("Remove an instance from Docker Desktop.");
    });
    _ = config.AddBranch("packs", packs =>
    {
        _ = packs.AddCommand<PacksStatusCommand>("status")
            .WithDescription("Show installed and available packs.");
        _ = packs.AddCommand<PacksApplyCommand>("apply")
            .WithDescription("Install or remove packs.");
        _ = packs.AddBranch("catalog", catalog =>
        {
            _ = catalog.AddCommand<PacksCatalogListCommand>("list")
                .WithDescription("List catalog packs.");
            _ = catalog.AddCommand<PacksCatalogEditCommand>("edit")
                .WithDescription("Print a catalog pack folder.");
            _ = catalog.AddCommand<PacksCatalogDuplicateCommand>("duplicate")
                .WithDescription("Duplicate a catalog pack.");
            _ = catalog.AddCommand<PacksCatalogDeleteCommand>("delete")
                .WithDescription("Delete a catalog pack.");
        });
    });
});

if (args.Length == 0)
{
    return Console.IsInputRedirected
        ? await app.RunAsync(["--help"]).ConfigureAwait(false)
        : await new InteractiveMenu(AnsiConsole.Console).RunAsync(CancellationToken.None).ConfigureAwait(false);
}

return await app.RunAsync(args).ConfigureAwait(false);
