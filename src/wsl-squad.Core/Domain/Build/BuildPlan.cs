namespace wsl_squad.Core.Domain.Build;

/// <summary>Validated, normalized build description.</summary>
public sealed record BuildPlan(
    string Name,
    string User,
    BuildSourceKind SourceKind,
    string BuildId,
    string InstallPath,
    string StagingDirectory,
    string? ImageTag,
    string? DockerfilePath,
    string? ContextPath,
    string? ImagePath,
    string? RegistryReference,
    string? OnboardingPath);

/// <summary>Result of a successful build.</summary>
public sealed record BuildSummary(
    string Name,
    string InstallPath,
    string? ImageTag,
    string? SavedImagePath,
    bool Onboarded);
