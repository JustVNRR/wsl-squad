using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Presentation.Formatting;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

/// <summary>Lists archives under the fleet root.</summary>
public sealed class ArchivesListCommand : Command<EmptyCommandSettings>
{
    public override int Execute(
        CommandContext context,
        EmptyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        _ = settings;
        cancellationToken.ThrowIfCancellationRequested();

        OperationResult<ArchiveListing> result = CliComposition.ListArchives();
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        if (result.Value.Archives.Count == 0)
        {
            AnsiConsole.Console.WriteLine("No archives.");
            return 0;
        }

        Table table = new();
        table.AddColumn("Name");
        table.AddColumn("Size");
        table.AddColumn("Format");
        table.AddColumn("Archived");
        table.AddColumn("File");

        foreach (WslArchiveInfo archive in result.Value.Archives)
        {
            table.AddRow(
                archive.Name,
                SizeFormatter.Format(archive.SizeBytes),
                archive.Format,
                archive.ArchivedAt.ToString("u", System.Globalization.CultureInfo.InvariantCulture),
                archive.FileName);
        }

        AnsiConsole.Console.Write(table);

        foreach (string warning in result.Value.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return 0;
    }
}

public sealed class DeleteArchiveSettings : CommandSettings
{
    [CommandArgument(0, "<ARCHIVE>")]
    [Description("Archive to delete.")]
    public string Archive { get; init; } = string.Empty;

    [CommandOption("--yes")]
    [Description("Confirm this destructive operation.")]
    public bool Yes { get; init; }
}

/// <summary>Deletes an archive folder.</summary>
public sealed class DeleteArchiveCommand : Command<DeleteArchiveSettings>
{
    public override int Execute(
        CommandContext context,
        DeleteArchiveSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        FleetOutput output = new(AnsiConsole.Console);

        if (!settings.Yes)
        {
            OperationResult<DeleteArchiveOutcome> refusal = OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.ConfirmationRequired,
                "Deleting an archive is destructive. Re-run with --yes.");
            output.WriteError(refusal.Error!);

            return ExitCodeMapper.Map(refusal);
        }

        OperationResult<DeleteArchiveOutcome> result = CliComposition.DeleteArchive(settings.Archive);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Deleted archive '{result.Value.ArchiveName}'.");

        return ExitCodeMapper.Map(result);
    }
}
