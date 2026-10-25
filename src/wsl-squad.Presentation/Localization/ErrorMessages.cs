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
            WslSquadErrorCode.InvalidFleetTarget => Localizer.Instance["ErrorInvalidFleetTarget"],
            WslSquadErrorCode.InstanceAlreadyExists =>
                Localizer.Instance.Format("ErrorInstanceAlreadyExists", target),
            WslSquadErrorCode.InstallFolderNotEmpty =>
                Localizer.Instance.Format("ErrorInstallFolderNotEmpty", target),
            WslSquadErrorCode.DockerUnavailable => Localizer.Instance["ErrorDockerUnavailable"],
            WslSquadErrorCode.DockerOperationFailed =>
                Localizer.Instance.Format("ErrorDockerOperationFailed", detail),
            WslSquadErrorCode.DockerSettingsUnreadable => Localizer.Instance["ErrorDockerSettingsUnreadable"],
            WslSquadErrorCode.DockerSettingsWriteFailed =>
                Localizer.Instance.Format("ErrorDockerSettingsWriteFailed", target, detail),
            WslSquadErrorCode.DockerRestartFailed =>
                Localizer.Instance.Format("ErrorDockerRestartFailed", target),
            WslSquadErrorCode.InvalidBuildSource =>
                Localizer.Instance.Format("ErrorInvalidBuildSource", target),
            WslSquadErrorCode.InstanceWriteFailed =>
                Localizer.Instance.Format("ErrorInstanceWriteFailed", target, detail),
            WslSquadErrorCode.RestartStopFailed =>
                Localizer.Instance.Format("ErrorRestartStopFailed", target, detail),
            WslSquadErrorCode.RestartStartFailed =>
                Localizer.Instance.Format("ErrorRestartStartFailed", target, detail),
            WslSquadErrorCode.ArchiveFailed =>
                Localizer.Instance.Format("ErrorArchiveFailed", detail),
            WslSquadErrorCode.CompactFailed =>
                Localizer.Instance.Format("ErrorCompactFailed", detail),
            WslSquadErrorCode.TerminalProfileMissing =>
                Localizer.Instance.Format("ErrorTerminalProfileMissing", target),
            WslSquadErrorCode.PackManagerUnavailable =>
                Localizer.Instance.Format("ErrorPackManagerUnavailable", target),
            WslSquadErrorCode.InsufficientDiskSpace =>
                Localizer.Instance.Format("ErrorInsufficientDiskSpace", target, detail),
            WslSquadErrorCode.InstanceRunning =>
                Localizer.Instance.Format("ErrorInstanceRunning", target),
            WslSquadErrorCode.ArchiveMissing =>
                Localizer.Instance.Format("ErrorArchiveMissing", target),
            WslSquadErrorCode.ArchiveFolderExists =>
                Localizer.Instance.Format("ErrorArchiveFolderExists", target),
            WslSquadErrorCode.TargetFolderExists =>
                Localizer.Instance.Format("ErrorTargetFolderExists", target),
            WslSquadErrorCode.ArchiveTarMissing =>
                Localizer.Instance.Format("ErrorArchiveTarMissing", target),
            WslSquadErrorCode.WorkingFolderLost =>
                Localizer.Instance.Format("ErrorWorkingFolderLost", target),
            WslSquadErrorCode.PackNotFound =>
                Localizer.Instance.Format("ErrorPackNotFound", target),
            WslSquadErrorCode.PackAlreadyExists =>
                Localizer.Instance.Format("ErrorPackAlreadyExists", target),
            WslSquadErrorCode.RecipeSlotNotFound =>
                Localizer.Instance.Format("ErrorRecipeSlotNotFound", target),
            WslSquadErrorCode.FileOperationFailed => detail,
            WslSquadErrorCode.ExternalToolFailed =>
                string.IsNullOrEmpty(detail)
                    ? Localizer.Instance.Format("ErrorExternalToolFailed", target)
                    : detail,
            WslSquadErrorCode.WslImportFailed =>
                Localizer.Instance.Format("ErrorWslImportFailed", target, detail),
            WslSquadErrorCode.WslExportFailed =>
                Localizer.Instance.Format("ErrorWslExportFailed", target, detail),
            WslSquadErrorCode.WslUnregisterFailed =>
                Localizer.Instance.Format("ErrorWslUnregisterFailed", target, detail),
            WslSquadErrorCode.WslStartFailed =>
                Localizer.Instance.Format("ErrorWslStartFailed", target),
            WslSquadErrorCode.WslStopFailed =>
                Localizer.Instance.Format("ErrorWslStopFailed", target),
            WslSquadErrorCode.WslCompactFailed =>
                Localizer.Instance.Format("ErrorWslCompactFailed", target),
            WslSquadErrorCode.WslPackManagerCopyFailed =>
                Localizer.Instance.Format("ErrorWslPackManagerCopyFailed", target),
            WslSquadErrorCode.WslOperationFailed => detail,
            WslSquadErrorCode.EmbeddedResourceMissing =>
                Localizer.Instance.Format("ErrorEmbeddedResourceMissing", target),
            WslSquadErrorCode.PlatformUnsupported =>
                string.IsNullOrEmpty(detail)
                    ? Localizer.Instance["ErrorPlatformUnsupported"]
                    : detail,
            WslSquadErrorCode.CorruptState =>
                string.IsNullOrEmpty(detail)
                    ? Localizer.Instance["ErrorCorruptState"]
                    : detail,
            WslSquadErrorCode.NoUsableFont => Localizer.Instance["ErrorNoUsableFont"],
            _ => detail,
        };
    }
}
