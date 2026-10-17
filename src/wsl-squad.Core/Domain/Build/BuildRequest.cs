namespace wsl_squad.Core.Domain.Build;

/// <summary>Where a build takes its base image from.</summary>
public enum BuildSourceKind
{
    Dockerfile,
    Recipe,
    Image,
    Registry,
}

/// <summary>User request to build a new instance.</summary>
public sealed record BuildRequest
{
    public string Name { get; init; } = string.Empty;

    public string? User { get; init; }

    public BuildSourceKind SourceKind { get; init; }

    public string SourcePath { get; init; } = string.Empty;

    public string? Context { get; init; }

    public string? OnboardingPath { get; init; }

    public bool KeepImage { get; init; }

    public IReadOnlyList<string> Packs { get; init; } = [];
}
