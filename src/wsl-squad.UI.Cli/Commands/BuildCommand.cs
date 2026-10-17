using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.UI.Cli.Composition;
using wsl_squad.UI.Cli.Interaction;
using wsl_squad.UI.Cli.Views;

namespace wsl_squad.UI.Cli.Commands;

public sealed class BuildSettings : CommandSettings
{
    [CommandOption("--name <NAME>")]
    [Description("Name of the instance to build.")]
    public string Name { get; init; } = string.Empty;

    [CommandOption("--dockerfile <PATH>")]
    [Description("Dockerfile to build.")]
    public string? Dockerfile { get; init; }

    [CommandOption("--context <PATH>")]
    [Description("Docker build context (defaults to the Dockerfile folder).")]
    public string? Context { get; init; }

    [CommandOption("--recipe <NAME>")]
    [Description("Recipe from the catalog to build.")]
    public string? Recipe { get; init; }

    [CommandOption("--image <PATH>")]
    [Description("Saved Docker image archive to import.")]
    public string? Image { get; init; }

    [CommandOption("--registry <REFERENCE>")]
    [Description("Docker registry reference to import.")]
    public string? Registry { get; init; }

    [CommandOption("--user <USER>")]
    [Description("Default user for the instance.")]
    public string? User { get; init; }

    [CommandOption("--onboarding <PATH>")]
    [Description("Onboarding script to run inside the instance.")]
    public string? Onboarding { get; init; }

    [CommandOption("--keep-image")]
    [Description("Save the built image in the catalog before removing the tag.")]
    public bool KeepImage { get; init; }

    [CommandOption("--pack <PACKS>")]
    [Description("Comma-separated packs to install after the build.")]
    public string? Packs { get; init; }

    [CommandOption("--register-docker")]
    [Description("Register the instance with Docker Desktop after the build.")]
    public bool RegisterDocker { get; init; }
}

/// <summary>Builds a new WSL instance from an image source.</summary>
public sealed class BuildCommand : AsyncCommand<BuildSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        BuildSettings settings,
        CancellationToken cancellationToken)
    {
        _ = context;
        ArgumentNullException.ThrowIfNull(settings);

        OperationResult<BuildRequest> request = CreateRequest(settings);
        FleetOutput output = new(AnsiConsole.Console);

        if (!request.IsSuccess)
        {
            output.WriteError(request.Error!);
            return ExitCodeMapper.Map(request);
        }

        OperationResult<BuildSummary> result = await CliComposition
            .BuildInstanceAsync(request.Value, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            output.WriteError(result.Error!);
            return ExitCodeMapper.Map(result);
        }

        WriteSummary(result.Value);

        foreach (string warning in result.Warnings)
        {
            AnsiConsole.Console.WriteLine($"warning: {warning}");
        }

        return ExitCodeMapper.Map(result);
    }

    private static OperationResult<BuildRequest> CreateRequest(BuildSettings settings)
    {
        List<(BuildSourceKind Kind, string Path)> selected = [];

        if (!string.IsNullOrWhiteSpace(settings.Dockerfile))
        {
            selected.Add((BuildSourceKind.Dockerfile, settings.Dockerfile));
        }

        if (!string.IsNullOrWhiteSpace(settings.Recipe))
        {
            selected.Add((BuildSourceKind.Recipe, settings.Recipe));
        }

        if (!string.IsNullOrWhiteSpace(settings.Image))
        {
            selected.Add((BuildSourceKind.Image, settings.Image));
        }

        if (!string.IsNullOrWhiteSpace(settings.Registry))
        {
            selected.Add((BuildSourceKind.Registry, settings.Registry));
        }

        if (selected.Count != 1)
        {
            return OperationResult<BuildRequest>.Fail(
                ErrorCode.Usage,
                "Exactly one of --dockerfile, --recipe, --image or --registry is required.");
        }

        (BuildSourceKind kind, string sourcePath) = selected[0];

        return OperationResult<BuildRequest>.Ok(new BuildRequest
        {
            Name = settings.Name,
            User = settings.User,
            SourceKind = kind,
            SourcePath = sourcePath,
            Context = settings.Context,
            OnboardingPath = settings.Onboarding,
            KeepImage = settings.KeepImage,
            Packs = string.IsNullOrWhiteSpace(settings.Packs)
                ? []
                : [.. settings.Packs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
            RegisterDocker = settings.RegisterDocker,
        });
    }

    private static void WriteSummary(BuildSummary summary)
    {
        AnsiConsole.Console.WriteLine($"Instance '{summary.Name}' built at {summary.InstallPath}.");

        if (summary.ImageTag is not null)
        {
            AnsiConsole.Console.WriteLine($"Image tag: {summary.ImageTag}");
        }

        if (summary.SavedImagePath is not null)
        {
            AnsiConsole.Console.WriteLine($"Saved image: {summary.SavedImagePath}");
        }

        if (summary.Onboarded)
        {
            AnsiConsole.Console.WriteLine("Onboarding completed.");
        }
    }
}
