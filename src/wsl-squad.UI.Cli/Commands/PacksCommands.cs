using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class PacksStatusSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance whose packs should be inspected.")]
    public string Instance { get; init; } = string.Empty;
}

/// <summary>Shows the packs installed in and offered to an instance.</summary>
public sealed class PacksStatusCommand : AsyncCommand<PacksStatusSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        PacksStatusSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<PackStatus> result = await CliComposition
            .PackStatusAsync(settings.Instance, cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        WritePacks("Installed", result.Value.Installed, result.Value.Catalog);
        WriteCandidates(result.Value);
        return ExitCodeMapper.Map(result);
    }

    private static void WritePacks(string title, IReadOnlyList<string> names, IReadOnlyList<WslPack> catalog)
    {
        if (names.Count == 0)
        {
            AnsiConsole.Console.WriteLine($"{title}: none.");
            return;
        }

        Table table = new() { Title = new TableTitle(title) };
        table.AddColumn("Pack");
        table.AddColumn("Family");
        table.AddColumn("Description");

        foreach (string name in names)
        {
            WslPack? pack = catalog.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            table.AddRow(name, pack?.Family ?? "-", pack?.Description ?? "-");
        }

        AnsiConsole.Console.Write(table);
    }

    private static void WriteCandidates(PackStatus status)
    {
        if (status.Candidates.Count == 0)
        {
            AnsiConsole.Console.WriteLine("Available: none.");
            return;
        }

        Table table = new() { Title = new TableTitle("Available") };
        table.AddColumn("Pack");
        table.AddColumn("Family");
        table.AddColumn("Description");

        foreach (WslPack pack in status.Candidates)
        {
            table.AddRow(pack.Name, pack.Family, pack.Description);
        }

        AnsiConsole.Console.Write(table);
    }
}

public sealed class PacksApplySettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to change.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--add <PACKS>")]
    [Description("Comma-separated packs to install.")]
    public string? Add { get; init; }

    [CommandOption("--remove <PACKS>")]
    [Description("Comma-separated packs to remove.")]
    public string? Remove { get; init; }
}

/// <summary>Applies pack additions and removals to an instance.</summary>
public sealed class PacksApplyCommand : AsyncCommand<PacksApplySettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        PacksApplySettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        IReadOnlyList<string> add = Split(settings.Add);
        IReadOnlyList<string> remove = Split(settings.Remove);

        if (add.Count == 0 && remove.Count == 0)
        {
            OperationResult<PackManageOutcome> usage = OperationResult<PackManageOutcome>.Fail(
                ErrorCode.Usage,
                "Provide --add and/or --remove.");
            new FleetOutput(AnsiConsole.Console).WriteError(usage.Error!);

            return ExitCodeMapper.Map(usage);
        }

        OperationResult<PackManageOutcome> result = await CliComposition
            .PackManageAsync(settings.Instance, add, remove, cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        if (result.Value.Added.Count > 0)
        {
            AnsiConsole.Console.WriteLine($"Added: {string.Join(", ", result.Value.Added)}");
        }

        if (result.Value.Removed.Count > 0)
        {
            AnsiConsole.Console.WriteLine($"Removed: {string.Join(", ", result.Value.Removed)}");
        }

        foreach (PackOperationFailure failure in result.Value.Failed)
        {
            AnsiConsole.Console.WriteLine(
                $"warning: {failure.PackName} ({failure.Step}): {failure.Message}");
        }

        return ExitCodeMapper.Map(result);
    }

    private static IReadOnlyList<string> Split(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}

/// <summary>Lists the packs in the editable catalog.</summary>
public sealed class PacksCatalogListCommand : Command<EmptyCommandSettings>
{
    public override int Execute(
        CommandContext context,
        EmptyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = settings;
        cancellationToken.ThrowIfCancellationRequested();

        OperationResult<IReadOnlyList<WslPack>> result = CliComposition.PackCatalogList();
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        Table table = new();
        table.AddColumn("Pack");
        table.AddColumn("Family");
        table.AddColumn("Visible");
        table.AddColumn("Description");

        foreach (WslPack pack in result.Value)
        {
            table.AddRow(pack.Name, pack.Family, pack.Visible ? "yes" : "no", pack.Description);
        }

        AnsiConsole.Console.Write(table);

        return ExitCodeMapper.Map(result);
    }
}

public sealed class PacksCatalogEditSettings : CommandSettings
{
    [CommandArgument(0, "<PACK>")]
    [Description("Pack whose folder should be shown.")]
    public string Pack { get; init; } = string.Empty;
}

/// <summary>Prints the folder of a catalog pack so it can be edited.</summary>
public sealed class PacksCatalogEditCommand : Command<PacksCatalogEditSettings>
{
    public override int Execute(
        CommandContext context,
        PacksCatalogEditSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<string> result = CliComposition.PackCatalogDirectory(settings.Pack);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(result.Value);

        return ExitCodeMapper.Map(result);
    }
}

public sealed class PacksCatalogDuplicateSettings : CommandSettings
{
    [CommandArgument(0, "<SOURCE>")]
    public string Source { get; init; } = string.Empty;

    [CommandArgument(1, "<TARGET>")]
    public string Target { get; init; } = string.Empty;
}

/// <summary>Duplicates a catalog pack.</summary>
public sealed class PacksCatalogDuplicateCommand : Command<PacksCatalogDuplicateSettings>
{
    public override int Execute(
        CommandContext context,
        PacksCatalogDuplicateSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<string> result = CliComposition.PackCatalogDuplicate(settings.Source, settings.Target);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Pack duplicated to {result.Value}.");

        return ExitCodeMapper.Map(result);
    }
}

public sealed class PacksCatalogDeleteSettings : CommandSettings
{
    [CommandArgument(0, "<PACK>")]
    public string Pack { get; init; } = string.Empty;

    [CommandOption("--yes")]
    public bool Yes { get; init; }
}

/// <summary>Deletes a pack from the editable catalog.</summary>
public sealed class PacksCatalogDeleteCommand : Command<PacksCatalogDeleteSettings>
{
    public override int Execute(
        CommandContext context,
        PacksCatalogDeleteSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        FleetOutput output = new(AnsiConsole.Console);

        if (!settings.Yes)
        {
            OperationResult refusal = OperationResult.Fail(
                ErrorCode.ConfirmationRequired,
                "Deleting a catalog pack is destructive. Re-run with --yes.");
            output.WriteError(refusal.Error!);

            return ExitCodeMapper.Map(refusal);
        }

        OperationResult result = CliComposition.PackCatalogDelete(settings.Pack);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Deleted catalog pack '{settings.Pack}'.");

        return ExitCodeMapper.Map(result);
    }
}
