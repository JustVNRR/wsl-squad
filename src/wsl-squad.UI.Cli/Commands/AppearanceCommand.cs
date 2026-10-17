using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class AppearanceSettings : CommandSettings
{
    [CommandArgument(0, "<INSTANCE>")]
    [Description("Instance whose appearance should be read or changed.")]
    public string Instance { get; init; } = string.Empty;

    [CommandOption("--font <FONT>")]
    public string? Font { get; init; }

    [CommandOption("--scheme <SCHEME>")]
    public string? ColorScheme { get; init; }

    [CommandOption("--tab-title <TITLE>")]
    public string? TabTitle { get; init; }

    [CommandOption("--icon-text <TEXT>")]
    public string? IconText { get; init; }

    [CommandOption("--icon-top <COLOR>")]
    public string? IconTop { get; init; }

    [CommandOption("--icon-bottom <COLOR>")]
    public string? IconBottom { get; init; }

    [CommandOption("--icon-color <COLOR>")]
    public string? IconTextColor { get; init; }

    [CommandOption("--icon-image <PATH>")]
    public string? IconImage { get; init; }

    public bool HasChanges =>
        Font is not null ||
        ColorScheme is not null ||
        TabTitle is not null ||
        IconText is not null ||
        IconTop is not null ||
        IconBottom is not null ||
        IconTextColor is not null ||
        IconImage is not null;
}

/// <summary>Shows or updates the terminal appearance of an instance.</summary>
public sealed class AppearanceCommand : Command<AppearanceSettings>
{
    public override int Execute(
        CommandContext context,
        AppearanceSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<AppearanceOutcome> result = settings.HasChanges
            ? CliComposition.AppearanceSet(
                settings.Instance,
                new AppearanceRequest(
                    settings.Font,
                    settings.ColorScheme,
                    settings.TabTitle,
                    settings.IconText,
                    settings.IconTop,
                    settings.IconBottom,
                    settings.IconTextColor,
                    settings.IconImage))
            : CliComposition.AppearanceGet(settings.Instance);

        FleetOutput output = new(AnsiConsole.Console);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        WriteAppearance(result.Value, settings.HasChanges);

        foreach (string warning in result.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }

    private static void WriteAppearance(AppearanceOutcome outcome, bool changed)
    {
        AnsiConsole.Console.WriteLine($"Instance: {outcome.InstanceName}");
        AnsiConsole.Console.WriteLine($"Font: {outcome.Font ?? "-"}");
        AnsiConsole.Console.WriteLine($"Colour scheme: {outcome.ColorScheme ?? "-"}");
        AnsiConsole.Console.WriteLine($"Tab title: {outcome.TabTitle ?? "-"}");
        AnsiConsole.Console.WriteLine($"Icon text: {outcome.Icon?.Text ?? "-"}");
        AnsiConsole.Console.WriteLine($"Icon top: {outcome.Icon?.Top ?? "-"}");
        AnsiConsole.Console.WriteLine($"Icon bottom: {outcome.Icon?.Bottom ?? "-"}");
        AnsiConsole.Console.WriteLine($"Icon text colour: {outcome.Icon?.TextColor ?? "-"}");
        AnsiConsole.Console.WriteLine($"Icon image: {outcome.Icon?.Image ?? "-"}");
        AnsiConsole.Console.WriteLine(
            $"Windows Terminal profile: {(outcome.HasProfile ? "found" : "not found")}");
        AnsiConsole.Console.WriteLine(changed ? "Appearance saved." : "Appearance unchanged.");
    }
}
