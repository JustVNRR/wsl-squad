using System.Text.Json;

using Spectre.Console;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Presentation.Dtos.Fleet;

namespace wsl_squad.UI.Cli.Views;

/// <summary>Renders fleet snapshots and errors for the CLI.</summary>
public sealed class FleetOutput
{
    private readonly IAnsiConsole _console;

    public FleetOutput(IAnsiConsole console)
    {
        _console = console;
    }

    public void WriteSnapshot(FleetSnapshotDto snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _console.WriteLine($"Fleet root: {snapshot.Root}");

        if (snapshot.Instances.Count == 0)
        {
            _console.WriteLine("No live instances.");
        }
        else
        {
            Table table = new();
            table.AddColumn("Name");
            table.AddColumn("Size");
            table.AddColumn("State");
            table.AddColumn("Docker");
            table.AddColumn("Path");

            foreach (InstanceRowDto instance in snapshot.Instances)
            {
                table.AddRow(
                    instance.Name,
                    instance.Size,
                    instance.IsRunning ? "running" : "stopped",
                    instance.Docker,
                    instance.Path);
            }

            _console.Write(table);
        }

        if (snapshot.Orphans.Count > 0)
        {
            Table orphans = new() { Title = new TableTitle("Orphans") };
            orphans.AddColumn("Name");
            orphans.AddColumn("Size");
            orphans.AddColumn("Path");

            foreach (OrphanRowDto orphan in snapshot.Orphans)
            {
                orphans.AddRow(orphan.Name, orphan.Size, orphan.Path);
            }

            _console.Write(orphans);
        }

        if (snapshot.Archives.Count > 0)
        {
            Table archives = new() { Title = new TableTitle("Archives") };
            archives.AddColumn("Name");
            archives.AddColumn("Size");
            archives.AddColumn("Format");
            archives.AddColumn("Archived");
            archives.AddColumn("File");

            foreach (ArchiveRowDto archive in snapshot.Archives)
            {
                archives.AddRow(
                    archive.Name,
                    archive.Size,
                    archive.Format,
                    archive.ArchivedAt.ToString("u", System.Globalization.CultureInfo.InvariantCulture),
                    archive.File);
            }

            _console.Write(archives);
        }

        foreach (string warning in snapshot.Warnings)
        {
            _console.WriteLine($"warning: {warning}");
        }
    }

    public void WriteJson(FleetSnapshotDto snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _console.Profile.Out.Writer.WriteLine(JsonSerializer.Serialize(snapshot, JsonDefaults.Options));
    }

    public void WriteError(OperationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _console.WriteLine($"error [{error.Code}]: {error.Message}");

        if (!string.IsNullOrWhiteSpace(error.Detail))
        {
            _console.WriteLine(error.Detail);
        }
    }
}
