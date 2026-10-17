using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class OpenSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to open.")]
    public string Instance { get; init; } = string.Empty;
}

/// <summary>Opens a shell for an instance.</summary>
public sealed class OpenCommand : Command<OpenSettings>
{
    public override int Execute(
        CommandContext context,
        OpenSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<OpenOutcome> result = CliComposition.Open(settings.Instance);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            result.Value.UsedTerminalProfile
                ? $"Opened the Windows Terminal profile '{settings.Instance}'."
                : $"Opened a shell for '{settings.Instance}'.");

        return ExitCodeMapper.Map(result);
    }
}

public sealed class RestartSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to restart.")]
    public string Instance { get; init; } = string.Empty;
}

/// <summary>Terminates and reopens an instance.</summary>
public sealed class RestartCommand : AsyncCommand<RestartSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RestartSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<RestartOutcome> result = await CliComposition
            .RestartAsync(settings.Instance, cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Restarted '{settings.Instance}'.");

        return ExitCodeMapper.Map(result);
    }
}
