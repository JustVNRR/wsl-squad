namespace wsl_squad.Core.Domain.Common;

/// <summary>Stable error categories returned by every operation.</summary>
public enum ErrorCode
{
    Usage,
    InvalidName,
    NotFound,
    AlreadyExists,
    InvalidState,
    Validation,
    ConfirmationRequired,
    DiskSpace,
    ExternalTool,
    Timeout,
    Cancelled,
    CorruptState,
    PackConflict,
    DockerUnavailable,
    TerminalUnavailable,
    MigrationBlocked,
    Internal,
}
