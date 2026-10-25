namespace WslSquad.Core.Exceptions;

public enum WslSquadErrorCode
{
    InvalidFleetTarget,
    InstanceAlreadyExists,
    InstallFolderNotEmpty,
    DockerUnavailable,
    DockerOperationFailed,
    DockerSettingsUnreadable,
    DockerSettingsWriteFailed,
    DockerRestartFailed,
    InvalidBuildSource,
    InstanceWriteFailed,
    RestartStopFailed,
    RestartStartFailed,
    ArchiveFailed,
    CompactFailed,
    TerminalProfileMissing,
    PackManagerUnavailable,
    InsufficientDiskSpace,
    InstanceRunning,
    ArchiveMissing,
    ArchiveFolderExists,
    TargetFolderExists,
    ArchiveTarMissing,
    WorkingFolderLost,
    PackNotFound,
    PackAlreadyExists,
    RecipeSlotNotFound,
    FileOperationFailed,
    ExternalToolFailed,
    WslImportFailed,
    WslExportFailed,
    WslUnregisterFailed,
    WslStartFailed,
    WslStopFailed,
    WslCompactFailed,
    WslPackManagerCopyFailed,
    WslOperationFailed,
    EmbeddedResourceMissing,
    PlatformUnsupported,
    CorruptState,
    NoUsableFont,
}

public class WslSquadException : InvalidOperationException
{
    public WslSquadException(
        WslSquadErrorCode code,
        string? target = null,
        string? technicalDetail = null,
        Exception? innerException = null)
        : base(BuildMessage(code, target, technicalDetail), innerException)
    {
        Code = code;
        Target = target;
        TechnicalDetail = technicalDetail;
    }

    public WslSquadErrorCode Code { get; }

    public string? Target { get; }

    public string? TechnicalDetail { get; }

    private static string BuildMessage(
        WslSquadErrorCode code,
        string? target,
        string? technicalDetail)
    {
        string message = code.ToString();

        if (!string.IsNullOrEmpty(target))
        {
            message += $" target='{target}'";
        }

        if (!string.IsNullOrEmpty(technicalDetail))
        {
            message += $" detail='{technicalDetail}'";
        }

        return message;
    }
}
