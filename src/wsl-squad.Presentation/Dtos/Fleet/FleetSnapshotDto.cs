namespace wsl_squad.Presentation.Dtos.Fleet;

/// <summary>Read-only fleet snapshot prepared for display.</summary>
public sealed record FleetSnapshotDto(
    string Root,
    IReadOnlyList<InstanceRowDto> Instances,
    IReadOnlyList<ArchiveRowDto> Archives,
    IReadOnlyList<OrphanRowDto> Orphans,
    IReadOnlyList<string> Warnings);

/// <summary>One live instance row.</summary>
public sealed record InstanceRowDto(
    string Name,
    string Path,
    bool IsRunning,
    string Docker,
    long SizeBytes,
    string Size);

/// <summary>One archive row.</summary>
public sealed record ArchiveRowDto(
    string Name,
    string Path,
    string File,
    string Format,
    long SizeBytes,
    string Size,
    DateTimeOffset ArchivedAt);

/// <summary>One orphan folder row.</summary>
public sealed record OrphanRowDto(string Name, string Path, long SizeBytes, string Size);
