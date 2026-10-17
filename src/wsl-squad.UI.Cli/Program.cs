using Spectre.Console.Cli;

using wsl_squad.UI.Cli.Commands;

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
});

return await app.RunAsync(args).ConfigureAwait(false);
