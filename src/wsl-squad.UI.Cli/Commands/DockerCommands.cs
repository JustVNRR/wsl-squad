using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Application.Docker;
using wsl_squad.Core.Domain.Common;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class DockerSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to register or unregister.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--restart")]
    [Description("Restart Docker Desktop after the change.")]
    public bool Restart { get; init; }
}

/// <summary>Registers an instance with Docker Desktop.</summary>
public sealed class DockerRegisterCommand : AsyncCommand<DockerSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DockerSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<DockerIntegrationOutcome> result = await CliComposition
            .DockerSetAsync(settings.Instance, integrated: true, settings.Restart, cancellationToken)
            .ConfigureAwait(false);

        return Write(result, settings.Instance, "Registered", "Docker Desktop integration");
    }

    private static int Write(
        OperationResult<DockerIntegrationOutcome> result,
        string instanceName,
        string verb,
        string label)
    {
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"{verb} '{instanceName}' ({label}: {result.Value.State}).");

        foreach (string warning in result.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }
}

/// <summary>Removes an instance from Docker Desktop.</summary>
public sealed class DockerUnregisterCommand : AsyncCommand<DockerSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DockerSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<DockerIntegrationOutcome> result = await CliComposition
            .DockerSetAsync(settings.Instance, integrated: false, settings.Restart, cancellationToken)
            .ConfigureAwait(false);

        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Unregistered '{settings.Instance}' (Docker Desktop integration: {result.Value.State}).");

        foreach (string warning in result.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }
}
