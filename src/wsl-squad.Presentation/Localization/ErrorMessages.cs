using WslSquad.Core.Exceptions;

namespace WslSquad.Presentation.Localization;

public static class ErrorMessages
{
    public static string ToMessage(this Exception exception)
    {
        return exception is InvalidOperationException operation
            ? operation.ToMessage()
            : exception.Message;
    }

    public static string ToMessage(this InvalidOperationException exception)
    {
        return exception is WslSquadException specific
            ? specific.ToMessage()
            : exception.Message;
    }

    public static string ToMessage(this WslSquadException exception)
    {
        string target = exception.Target ?? string.Empty;
        string detail = exception.TechnicalDetail ?? string.Empty;

        return exception.Code switch
        {
            WslSquadErrorCode.InvalidFleetTarget => "Invalid fleet target.",
            WslSquadErrorCode.InstanceAlreadyExists => $"An instance named '{target}' already exists.",
            WslSquadErrorCode.InstallFolderNotEmpty =>
                $"Installation folder '{target}' already exists and is not empty.",
            WslSquadErrorCode.DockerUnavailable =>
                "Docker Desktop is not running and could not be started.",
            WslSquadErrorCode.DockerOperationFailed => $"Docker failed: {detail}",
            WslSquadErrorCode.DockerSettingsUnreadable =>
                "Docker Desktop's settings file could not be read.",
            WslSquadErrorCode.DockerSettingsWriteFailed =>
                $"Docker Desktop's settings could not take '{target}': {detail}",
            WslSquadErrorCode.DockerRestartFailed =>
                $"Docker Desktop would not restart to pick '{target}' up.",
            WslSquadErrorCode.InvalidBuildSource =>
                $"The recipe carries no Dockerfile: '{target}'.",
            WslSquadErrorCode.InstanceWriteFailed =>
                $"The instance file could not be written for '{target}': {detail}",
            WslSquadErrorCode.RestartStopFailed =>
                $"The stop did not complete - '{target}' may still be running, and was not started again. {detail}",
            WslSquadErrorCode.RestartStartFailed =>
                $"The start did not complete - '{target}' is stopped. {detail}",
            WslSquadErrorCode.ArchiveFailed =>
                $"The archive did not complete - nothing was destroyed. {detail}",
            WslSquadErrorCode.CompactFailed =>
                $"The archive did not complete - nothing was compacted: {detail}",
            WslSquadErrorCode.TerminalProfileMissing =>
                $"Windows Terminal has no profile for '{target}' - the look cannot be applied.",
            WslSquadErrorCode.PackManagerUnavailable =>
                $"No pack can be installed or removed on '{target}'.",
            WslSquadErrorCode.InsufficientDiskSpace => $"Not enough room on {target}: {detail}.",
            WslSquadErrorCode.InstanceRunning =>
                $"'{target}' is running, and stopping it was not allowed: an archive reads a still disk.",
            WslSquadErrorCode.ArchiveMissing => $"There is no archive named '{target}'.",
            WslSquadErrorCode.ArchiveFolderExists =>
                $"'{target}' already exists - move it away first.",
            WslSquadErrorCode.TargetFolderExists =>
                $"A folder with that name already exists: {target}",
            WslSquadErrorCode.ArchiveTarMissing =>
                $"No tar in the archive folder: {target}",
            WslSquadErrorCode.WorkingFolderLost =>
                $"The working folder '{target}' is gone, and no existing folder holds your instances.",
            WslSquadErrorCode.PackNotFound =>
                $"There is no pack named '{target}' in the catalogue.",
            WslSquadErrorCode.PackAlreadyExists =>
                $"A pack named '{target}' already lives in the catalogue.",
            WslSquadErrorCode.RecipeSlotNotFound =>
                $"'{target}' is not a recipe slot - nothing was removed.",
            WslSquadErrorCode.FileOperationFailed => detail,
            WslSquadErrorCode.ExternalToolFailed =>
                string.IsNullOrEmpty(detail) ? $"'{target}' could not be started." : detail,
            WslSquadErrorCode.WslImportFailed => $"'{target}' could not be imported. {detail}",
            WslSquadErrorCode.WslExportFailed => $"'{target}' could not be exported. {detail}",
            WslSquadErrorCode.WslUnregisterFailed => $"'{target}' could not be unregistered. {detail}",
            WslSquadErrorCode.WslStartFailed => $"'{target}' could not be started.",
            WslSquadErrorCode.WslStopFailed => $"'{target}' could not be stopped.",
            WslSquadErrorCode.WslCompactFailed => $"The compact failed for '{target}'.",
            WslSquadErrorCode.WslPackManagerCopyFailed =>
                $"The pack manager could not be placed in '{target}'.",
            WslSquadErrorCode.WslOnboardingCopyFailed =>
                $"'{target}' could not be placed as /root/onboarding.sh.",
            WslSquadErrorCode.WslOperationFailed => $"{detail}",
            WslSquadErrorCode.EmbeddedResourceMissing =>
                $"The embedded resource '{target}' is missing.",
            WslSquadErrorCode.PlatformUnsupported =>
                string.IsNullOrEmpty(detail) ? "This operation is not supported on this platform." : detail,
            WslSquadErrorCode.CorruptState =>
                string.IsNullOrEmpty(detail) ? "The file is corrupt." : detail,
            WslSquadErrorCode.NoUsableFont =>
                "No usable font family found on this machine.",
            _ => detail,
        };
    }
}
