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
});

return await app.RunAsync(args).ConfigureAwait(false);
