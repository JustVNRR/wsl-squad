using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Environment;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class InitSettings : CommandSettings
{
    [CommandOption("--root <PATH>")]
    [Description("Fleet root to create or adopt. Defaults to the resolved root.")]
    public string? Root { get; init; }
}

/// <summary>Creates or adopts a fleet root and writes config.json.</summary>
public sealed class InitCommand : Command<InitSettings>
{
    public override int Execute(
        CommandContext context,
        InitSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        OperationResult<string> result = CliComposition.InitializeFleetRoot(settings.Root);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        SystemPlatformEnvironment platform = new();
        output.WriteSnapshot(new Presentation.Dtos.Fleet.FleetSnapshotDto(
            result.Value,
            [],
            [],
            [],
            []));
        AnsiConsole.Console.WriteLine($"Configuration: {AppPaths.GetConfigFilePath(platform)}");

        return ExitCodeMapper.Map(result);
    }
}
