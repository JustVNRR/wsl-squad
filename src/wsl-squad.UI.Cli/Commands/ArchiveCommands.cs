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

public sealed class ArchiveCreateSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance to archive.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--name <ARCHIVE>")]
    [Description("Archive name (defaults to <instance>_archive).")]
    public string? Name { get; init; }

    [CommandOption("--format <FORMAT>")]
    [Description("Archive format: tar or tar.gz (default).")]
    public string Format { get; init; } = ArchiveFormat.Default;

    [CommandOption("--replace")]
    [Description("Replace an existing archive.")]
    public bool Replace { get; init; }

    [CommandOption("--stop")]
    [Description("Stop the instance if it is running.")]
    public bool Stop { get; init; }
}

/// <summary>Archives an instance.</summary>
public sealed class ArchiveCreateCommand : AsyncCommand<ArchiveCreateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ArchiveCreateSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<ArchiveOutcome> result = await CliComposition
            .ArchiveAsync(
                new ArchiveRequest(settings.Instance, settings.Name, settings.Format, settings.Replace, settings.Stop),
                cancellationToken)
            .ConfigureAwait(false);
        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        AnsiConsole.Console.WriteLine(
            $"Archive '{result.Value.ArchiveName}' written to {result.Value.ArchiveDirectory} " +
            $"({SizeFormatter.Format(result.Value.SizeBytes)}).");

        foreach (string warning in result.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }
}
