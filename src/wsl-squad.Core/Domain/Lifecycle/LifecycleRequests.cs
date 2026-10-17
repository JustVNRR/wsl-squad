namespace wsl_squad.Core.Domain.Lifecycle;

/// <summary>Supported WSL export formats.</summary>
public static class ArchiveFormat
{
    public const string Default = "tar.gz";

    private static readonly string[] _supported = ["tar", "tar.gz"];

    public static bool IsSupported(string format)
    {
        return _supported.Contains(format, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>Request to archive an instance.</summary>
public sealed record ArchiveRequest(
    string InstanceName,
    string? ArchiveName,
    string Format,
    bool Replace,
    bool StopIfRunning);

/// <summary>Request to restore an instance from an archive.</summary>
public sealed record RestoreRequest(string ArchiveName, string InstanceName);

/// <summary>Request to duplicate an instance.</summary>
public sealed record DuplicateRequest(string SourceName, string TargetName);

/// <summary>Request to remove an instance.</summary>
public sealed record RemoveRequest(string InstanceName, bool ArchiveFirst);

/// <summary>Request to compact an instance.</summary>
public sealed record CompactRequest(string InstanceName, bool ArchiveFirst);

/// <summary>Outcome of an archive operation.</summary>
public sealed record ArchiveOutcome(
    string InstanceName,
    string ArchiveName,
    string ArchiveDirectory,
    string ArchiveFile,
    string Format,
    long SizeBytes,
    bool Restarted);

/// <summary>Outcome of a restore operation.</summary>
public sealed record RestoreOutcome(string InstanceName, string ArchiveName, string InstallPath, string DefaultUser);

/// <summary>Outcome of a duplicate operation.</summary>
public sealed record DuplicateOutcome(string SourceName, string TargetName, string InstallPath);

/// <summary>Outcome of a remove operation.</summary>
public sealed record RemoveOutcome(string InstanceName, bool Archived, IReadOnlyList<string> Warnings);

/// <summary>Outcome of a compact operation.</summary>
public sealed record CompactOutcome(
    string InstanceName,
    long SizeBefore,
    long SizeAfter,
    long Gained,
    bool Restarted);

/// <summary>Outcome of deleting an archive.</summary>
public sealed record DeleteArchiveOutcome(string ArchiveName);

/// <summary>Outcome of opening a shell.</summary>
public sealed record OpenOutcome(string InstanceName, bool UsedTerminalProfile);

/// <summary>Outcome of restarting an instance.</summary>
public sealed record RestartOutcome(string InstanceName, bool UsedTerminalProfile);
