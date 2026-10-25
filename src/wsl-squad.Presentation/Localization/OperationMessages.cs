using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Localization;

public static class OperationMessages
{
    public static string ToMessage(this BuildNoticeDto notice)
    {
        return notice.Kind switch
        {
            BuildNoticeKind.LookFailed =>
                Localizer.Instance.Format("NoticeLookFailed", notice.TechnicalDetail),
            BuildNoticeKind.ImageSaveFailed =>
                Localizer.Instance.Format("NoticeImageSaveFailed", notice.Subject),
            BuildNoticeKind.FontInstallFailed =>
                Localizer.Instance.Format("NoticeFontInstallFailed", notice.Subject, notice.TechnicalDetail),
            BuildNoticeKind.NoIconGenerated =>
                Localizer.Instance.Format("NoticeNoIcon", notice.TechnicalDetail),
            BuildNoticeKind.GhostEntriesNotPruned =>
                Localizer.Instance.Format("NoticeGhostsNotPruned", notice.Subject),
            BuildNoticeKind.NoWslFragment =>
                Localizer.Instance.Format("NoticeNoWslFragment", notice.Subject),
            BuildNoticeKind.ShellOpenFailed =>
                Localizer.Instance.Format("NoticeShellOpenFailed", notice.TechnicalDetail),
            _ => string.Empty,
        };
    }

    public static string ToMessage(this AppearanceChangeDto change)
    {
        return change.Kind switch
        {
            AppearanceChangeKind.Font =>
                Localizer.Instance.Format("AppearanceChangeFont", change.Value),
            AppearanceChangeKind.ColourScheme =>
                Localizer.Instance.Format("AppearanceChangeColours", change.Value),
            AppearanceChangeKind.Icon => Localizer.Instance["AppearanceChangeIcon"],
            AppearanceChangeKind.IconImage => Localizer.Instance["AppearanceChangeIconImage"],
            _ => string.Empty,
        };
    }

    public static string ToMessage(this DockerRestartOutcome outcome)
    {
        return outcome switch
        {
            DockerRestartOutcome.NotRequested => Localizer.Instance["DockerRestartNotRequested"],
            DockerRestartOutcome.Restarted => Localizer.Instance["DockerRestartRestarted"],
            DockerRestartOutcome.Started => Localizer.Instance["DockerRestartStarted"],
            DockerRestartOutcome.Pending => Localizer.Instance["DockerRestartPending"],
            _ => Localizer.Instance["DockerRestartNotRequested"],
        };
    }

}
