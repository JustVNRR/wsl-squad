using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Presenters.Fleet;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class ListSettings : CommandSettings
{
    [CommandOption("--json")]
    [Description("Write the snapshot as JSON.")]
    public bool Json { get; init; }

    [CommandOption("--archives")]
    [Description("Include archives in the listing.")]
    public bool IncludeArchives { get; init; }
}

/// <summary>Lists managed instances, orphans and optionally archives.</summary>
public sealed class ListCommand : AsyncCommand<ListSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ListSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        FleetListPresenter presenter = CliComposition.CreateListPresenter();
        OperationResult<FleetSnapshotDto> result = await presenter
            .LoadAsync(settings.IncludeArchives, cancellationToken)
            .ConfigureAwait(false);

        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        if (settings.Json)
        {
            output.WriteJson(result.Value);
        }
        else
        {
            output.WriteSnapshot(result.Value);
        }

        return ExitCodeMapper.Map(result);
    }
}
