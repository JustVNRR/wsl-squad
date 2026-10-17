using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Presentation.Formatting;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class DuplicateSettings : CommandSettings
{
    [CommandArgument(0, "<SOURCE>")]
    [Description("Instance to copy.")]
    public string Source { get; init; } = string.Empty;

    [CommandArgument(1, "<TARGET>")]
    [Description("Name of the copy.")]
    public string Target { get; init; } = string.Empty;
}

/// <summary>Duplicates an instance under another name.</summary>
public sealed class DuplicateCommand : AsyncCommand<DuplicateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DuplicateSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<DuplicateOutcome> result = await CliComposition
            .DuplicateAsync(new DuplicateRequest(settings.Source, settings.Target), cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Instance '{result.Value.TargetName}' duplicated at {result.Value.InstallPath}.");

        return ExitCodeMapper.Map(result);
    }
}

public sealed class RestoreSettings : CommandSettings
{
    [CommandArgument(0, "<ARCHIVE>")]
    [Description("Archive to restore.")]
    public string Archive { get; init; } = string.Empty;

    [CommandOption("--name <INSTANCE>")]
    [Description("Name of the restored instance.")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>Restores an instance from an archive.</summary>
public sealed class RestoreCommand : AsyncCommand<RestoreSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RestoreSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<RestoreOutcome> result = await CliComposition
            .RestoreAsync(new RestoreRequest(settings.Archive, settings.Name), cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Instance '{result.Value.InstanceName}' restored at {result.Value.InstallPath}.");

        return ExitCodeMapper.Map(result);
    }
}

public sealed class RemoveSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to remove.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--archive-first")]
    [Description("Archive the instance before removing it.")]
    public bool ArchiveFirst { get; init; }

    [CommandOption("--yes")]
    [Description("Confirm this destructive operation.")]
    public bool Yes { get; init; }
}

/// <summary>Removes an instance, optionally archiving it first.</summary>
public sealed class RemoveCommand : AsyncCommand<RemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RemoveSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        FleetOutput output = new(AnsiConsole.Console);

        if (!settings.Yes)
        {
            OperationResult<RemoveOutcome> refusal = OperationResult<RemoveOutcome>.Fail(
                ErrorCode.ConfirmationRequired,
                "Removing an instance is destructive. Re-run with --yes.");
            output.WriteError(refusal.Error!);

            return ExitCodeMapper.Map(refusal);
        }

        OperationResult<RemoveOutcome> result = await CliComposition
            .RemoveAsync(new RemoveRequest(settings.Instance, settings.ArchiveFirst), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Removed '{result.Value.InstanceName}'.");

        foreach (string warning in result.Value.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }
}

public sealed class CompactSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to compact.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--archive-first")]
    [Description("Archive the instance before compacting it.")]
    public bool ArchiveFirst { get; init; }
}

/// <summary>Compacts an instance virtual disk.</summary>
public sealed class CompactCommand : AsyncCommand<CompactSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CompactSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<CompactOutcome> result = await CliComposition
            .CompactAsync(new CompactRequest(settings.Instance, settings.ArchiveFirst), cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Compacted '{result.Value.InstanceName}': {SizeFormatter.Format(result.Value.SizeBefore)} -> " +
            $"{SizeFormatter.Format(result.Value.SizeAfter)} (gained {SizeFormatter.Format(result.Value.Gained)}).");

        return ExitCodeMapper.Map(result);
    }
}
