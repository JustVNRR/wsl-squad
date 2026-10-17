namespace wsl_squad.Core.Domain.Fleet;

/// <summary>Read-only view of the fleet at a given root.</summary>
public sealed record FleetSnapshot(
    string Root,
    IReadOnlyList<WslInstanceInfo> Instances,
    IReadOnlyList<WslArchiveInfo> Archives,
    IReadOnlyList<WslOrphanInfo> Orphans,
    IReadOnlyList<string> Warnings);

/// <summary>A managed WSL distribution.</summary>
public sealed record WslInstanceInfo(
    string Name,
    string Path,
    bool IsRunning,
    string DockerDesktop,
    long SizeBytes,
    InstanceMetadata? Metadata);

/// <summary>An archive folder under the fleet root.</summary>
public sealed record WslArchiveInfo(
    string Name,
    string Path,
    string FileName,
    string Format,
    DateTimeOffset ArchivedAt,
    long SizeBytes,
    ArchiveMetadata Metadata);

/// <summary>A marked folder under the fleet root that no WSL distribution claims.</summary>
public sealed record WslOrphanInfo(string Name, string Path, long SizeBytes);

/// <summary>Archive listing with non-fatal warnings.</summary>
public sealed record ArchiveListing(IReadOnlyList<WslArchiveInfo> Archives, IReadOnlyList<string> Warnings);
