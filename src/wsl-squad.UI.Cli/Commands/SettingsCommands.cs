using System.ComponentModel;


using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Docker;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

/// <summary>Prints the resolved fleet root and configuration paths.</summary>
public sealed class RootShowCommand : Command<EmptyCommandSettings>
{
    public override int Execute(
        CommandContext context,
        EmptyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = settings;
        cancellationToken.ThrowIfCancellationRequested();

        CliContext cli = CliContext.Create();
        OperationResult<string> root = cli.ResolveRoot();

        if (!root.IsSuccess)
        {
            new FleetOutput(AnsiConsole.Console).WriteError(root.Error!);
            return ExitCodeMapper.Map(root);
        }

        OperationResult<FleetConfig?> config = cli.ConfigStore.Load();

        if (!config.IsSuccess)
        {
            new FleetOutput(AnsiConsole.Console).WriteError(config.Error!);
            return ExitCodeMapper.Map(config);
        }

        string catalogRoot = config.Value?.CatalogRoot
            ?? Path.Combine(AppPaths.GetConfigDirectory(cli.Platform), "catalog");

        AnsiConsole.Console.WriteLine($"Fleet root: {root.Value}");
        AnsiConsole.Console.WriteLine($"Configuration: {cli.ConfigStore.ConfigPath}");
        AnsiConsole.Console.WriteLine($"Catalog: {catalogRoot}");

        return 0;
    }
}

public sealed class RootMoveSettings : CommandSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("New fleet root.")]
    public string Path { get; init; } = string.Empty;

    [CommandOption("--yes")]
    [Description("Confirm moving the fleet; every instance is archived and recreated.")]
    public bool Yes { get; init; }
}

/// <summary>Moves the fleet to another folder.</summary>
public sealed class RootMoveCommand : AsyncCommand<RootMoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RootMoveSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);
        FleetOutput output = new(AnsiConsole.Console);

        if (!settings.Yes)
        {
            OperationResult<RootMoveOutcome> refusal = OperationResult<RootMoveOutcome>.Fail(
                ErrorCode.ConfirmationRequired,
                "Moving the fleet is destructive. Re-run with --yes.");
            output.WriteError(refusal.Error!);

            return ExitCodeMapper.Map(refusal);
        }

        OperationResult<RootMoveOutcome> result = await CliComposition
            .MoveRootAsync(settings.Path, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Moved {result.Value.MovedInstances.Count} instance(s) to {result.Value.TargetRoot}.");

        foreach (string warning in result.Value.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }
}

/// <summary>Creates the Windows-wide .wslconfig file when it is missing.</summary>
public sealed class WslConfigCommand : Command<EmptyCommandSettings>
{
    public override int Execute(
        CommandContext context,
        EmptyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = settings;
        cancellationToken.ThrowIfCancellationRequested();

        OperationResult<string> result = CliComposition.EnsureWslConfig();
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(result.Value);

        return 0;
    }
}

/// <summary>Runs basic environment checks.</summary>
public sealed class DoctorCommand : AsyncCommand<EmptyCommandSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        EmptyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = settings;

        CliContext cli = CliContext.Create();
        bool allGood = true;

        OperationResult<string> root = cli.ResolveRoot();
        allGood &= Report("Fleet root", root.IsSuccess, root.IsSuccess ? root.Value : root.Error!.Message);

        allGood &= Report(
            "Configuration",
            cli.FileSystem.File.Exists(cli.ConfigStore.ConfigPath),
            cli.ConfigStore.ConfigPath);

        OperationResult<IReadOnlyList<WslPack>> packs = CliComposition.PackCatalogList();
        allGood &= Report(
            "Pack catalog",
            packs.IsSuccess,
            packs.IsSuccess ? $"{packs.Value.Count} pack(s)" : packs.Error!.Message);

        bool registrySupported = cli.Registry.IsSupported;
        int distros = 0;

        if (registrySupported)
        {
            try
            {
                distros = cli.Registry.GetDistributions().Count;
            }
            catch (Exception exception) when (exception is PlatformNotSupportedException or IOException or UnauthorizedAccessException)
            {
                registrySupported = false;
            }
        }

        allGood &= Report("WSL registry", registrySupported, $"{distros} distribution(s)");

        DockerCliGateway docker = new(cli.ProcessRunner, cli.FileSystem);
        bool dockerAvailable = await docker.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
        allGood &= Report("Docker", dockerAvailable, dockerAvailable ? "available" : "not available");

        return allGood ? 0 : 8;
    }

    private static bool Report(string label, bool ok, string detail)
    {
        AnsiConsole.Console.WriteLine($"{(ok ? "ok  " : "fail")} {label}: {detail}");

        return ok;
    }
}
