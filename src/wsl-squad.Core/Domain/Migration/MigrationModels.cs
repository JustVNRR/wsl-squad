namespace wsl_squad.Core.Domain.Migration;

/// <summary>Metadata read from a legacy <c>instance.json</c>.</summary>
public sealed record LegacyMetadata(
    string DefaultUser,
    string? Font,
    string? ColorScheme,
    string? IconFrom,
    string? IconText,
    string? IconTop,
    string? IconBottom,
    string? IconTextColor,
    string Docker);

/// <summary>A legacy instance to adopt.</summary>
public sealed record MigrationInstance(
    string Name,
    string Directory,
    LegacyMetadata Metadata,
    bool MetadataReadable,
    string? Warning);

/// <summary>A legacy archive to convert.</summary>
public sealed record MigrationArchive(
    string Name,
    string Directory,
    string File,
    string Format,
    long SizeBytes,
    LegacyMetadata Metadata,
    string? Warning);

/// <summary>Plan produced by a migration dry run.</summary>
public sealed record MigrationPlan(
    string SourceRoot,
    IReadOnlyList<MigrationInstance> Instances,
    IReadOnlyList<MigrationArchive> Archives,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers);

/// <summary>Result of a completed migration.</summary>
public sealed record MigrationOutcome(
    string SourceRoot,
    string BackupDirectory,
    int Instances,
    int Archives,
    IReadOnlyList<string> Warnings);

/// <summary>Result of moving the fleet root.</summary>
public sealed record RootMoveOutcome(
    string SourceRoot,
    string TargetRoot,
    IReadOnlyList<string> MovedInstances,
    IReadOnlyList<string> Warnings);

/// <summary>Files copied aside before a migration.</summary>
public sealed class MigrationBackupEntry
{
    public string Original { get; set; } = string.Empty;

    public string Backup { get; set; } = string.Empty;
}

/// <summary>A file renamed by a migration.</summary>
public sealed class MigrationRenameEntry
{
    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;
}

/// <summary>Journal used to roll a migration back.</summary>
public sealed class MigrationManifest
{
    public string Id { get; set; } = string.Empty;

    public string SourceRoot { get; set; } = string.Empty;

    public List<MigrationBackupEntry> Backups { get; set; } = [];

    public List<string> Created { get; set; } = [];

    public List<MigrationRenameEntry> Renamed { get; set; } = [];
}
