using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class MigrateLegacySettings : CommandSettings
{
    [CommandOption("--source-root <PATH>")]
    [Description("Legacy fleet root (defaults to %LOCALAPPDATA%\\wsl-fleet\\root.txt).")]
    public string? SourceRoot { get; init; }

    [CommandOption("--dry-run")]
    [Description("Report what would be migrated without writing anything.")]
    public bool DryRun { get; init; }
}

/// <summary>Adopts a legacy .wsl-fleet fleet.</summary>
public sealed class MigrateLegacyCommand : Command<MigrateLegacySettings>
{
    public override int Execute(
        CommandContext context,
        MigrateLegacySettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        FleetOutput output = new(AnsiConsole.Console);

        if (settings.DryRun)
        {
            OperationResult<MigrationPlan> plan = CliComposition.CreateMigrationPlan(settings.SourceRoot);

            if (!plan.IsSuccess)
            {
                output.WriteError(plan.Error!);
                return ExitCodeMapper.Map(plan);
            }

            WritePlan(plan.Value);

            return plan.Value.Blockers.Count == 0 ? 0 : 8;
        }

        OperationResult<MigrationOutcome> migrated = CliComposition.MigrateLegacy(settings.SourceRoot);

        if (!migrated.IsSuccess)
        {
            output.WriteError(migrated.Error!);
            return ExitCodeMapper.Map(migrated);
        }

        AnsiConsole.Console.WriteLine($"Migrated {migrated.Value.Instances} instance(s) and " +
                                      $"{migrated.Value.Archives} archive(s).");
        AnsiConsole.Console.WriteLine($"Backup: {migrated.Value.BackupDirectory}");

        foreach (string warning in migrated.Value.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return 0;
    }

    private static void WritePlan(MigrationPlan plan)
    {
        AnsiConsole.Console.WriteLine($"Legacy root: {plan.SourceRoot}");
        AnsiConsole.Console.WriteLine($"Instances: {plan.Instances.Count}");
        AnsiConsole.Console.WriteLine($"Archives: {plan.Archives.Count}");

        foreach (MigrationInstance instance in plan.Instances)
        {
            AnsiConsole.Console.WriteLine($"  instance {instance.Name} ({instance.Directory})");
        }

        foreach (MigrationArchive archive in plan.Archives)
        {
            AnsiConsole.Console.WriteLine($"  archive {archive.Name} ({archive.File})");
        }

        foreach (string warning in plan.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        foreach (string blocker in plan.Blockers)
        {
            AnsiConsole.Console.WriteLine($"blocked: {blocker}");
        }
    }
}

public sealed class MigrateRollbackSettings : CommandSettings
{
    [CommandArgument(0, "<BACKUP-ID>")]
    [Description("Backup identifier printed by a migration.")]
    public string BackupId { get; init; } = string.Empty;
}

/// <summary>Restores the state captured by a migration backup.</summary>
public sealed class MigrateRollbackCommand : Command<MigrateRollbackSettings>
{
    public override int Execute(
        CommandContext context,
        MigrateRollbackSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult result = CliComposition.MigrateRollback(settings.BackupId);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine($"Rolled back migration '{settings.BackupId}'.");

        return 0;
    }
}
