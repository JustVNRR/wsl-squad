using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Localization;

public static class OperationMessages
{
    public static string ToMessage(this BuildNoticeDto notice)
    {
        return notice.Kind switch
        {
            BuildNoticeKind.LookFailed => $"the look failed: {notice.TechnicalDetail}",
            BuildNoticeKind.ImageSaveFailed => $"the image could not be saved to '{notice.Subject}'.",
            BuildNoticeKind.DockerReady => "Docker Desktop: ready - 'docker' works in this instance.",
            BuildNoticeKind.DockerNotAnswering =>
                "Docker Desktop: 'docker' does not answer in this instance yet." +
                Environment.NewLine +
                "  run 'docker version' in there; if it names the socket's permissions, restart Docker Desktop and open a new terminal.",
            BuildNoticeKind.FontInstallFailed =>
                $"the font '{notice.Subject}' would not install: {notice.TechnicalDetail}",
            BuildNoticeKind.NoIconGenerated => $"no icon ({notice.TechnicalDetail})",
            BuildNoticeKind.GhostEntriesNotPruned =>
                $"ghost entries NOT pruned in {notice.Subject} (unreadable JSON - a // comment breaks parsing; remove them by hand)",
            BuildNoticeKind.NoWslFragment =>
                $"no WSL fragment for '{notice.Subject}' - the profile was not applied",
            BuildNoticeKind.ShellOpenFailed =>
                $"the first shell did not open, so the onboarding did not run: {notice.TechnicalDetail}",
            _ => string.Empty,
        };
    }

    public static string ToMessage(this DockerRestartOutcome outcome)
    {
        return outcome switch
        {
            DockerRestartOutcome.NotRequested => "unchanged",
            DockerRestartOutcome.Restarted => "restarted",
            DockerRestartOutcome.Started => "started",
            DockerRestartOutcome.Pending => "restart pending",
            _ => "unchanged",
        };
    }

    public static string ToMessage(
        this RegistryValidationError error,
        string? technicalDetail = null)
    {
        return error switch
        {
            RegistryValidationError.None => string.Empty,
            RegistryValidationError.DockerUnavailable => "docker is not on the PATH.",
            RegistryValidationError.NotFound => "no such image or tag on Docker Hub.",
            RegistryValidationError.Rejected =>
                string.IsNullOrEmpty(technicalDetail)
                    ? "the registry rejected the image."
                    : technicalDetail,
            RegistryValidationError.Unreachable => "Docker Hub could not be reached.",
            _ => "the registry could not validate the image.",
        };
    }
}
