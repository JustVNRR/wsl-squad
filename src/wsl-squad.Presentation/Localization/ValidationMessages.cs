using WslSquad.Core.Services;
using WslSquad.Core.Validation;

namespace WslSquad.Presentation.Localization;

public static class ValidationMessages
{
    public static string? ToMessage(this NameProblem problem, string? attemptedName = null)
    {
        return problem switch
        {
            NameProblem.None => null,
            NameProblem.Empty => Localizer.Instance["ValidationNameEmpty"],
            NameProblem.InvalidFormat => Localizer.Instance["ValidationNameFormat"],
            NameProblem.AlreadyTaken => Localizer.Instance.Format("ValidationNameTaken", attemptedName),
            _ => Localizer.Instance["ValidationNameInvalid"],
        };
    }

    public static string? ToMessage(this FleetTargetProblem problem, string? target = null)
    {
        string path = target ?? string.Empty;
        string root = Path.GetPathRoot(path) ?? string.Empty;

        return problem switch
        {
            FleetTargetProblem.None => null,
            FleetTargetProblem.Empty => Localizer.Instance["ValidationFleetEmpty"],
            FleetTargetProblem.NotAbsolutePath =>
                Localizer.Instance.Format("ValidationFleetNotAbsolute", path),
            FleetTargetProblem.DriveUnavailable =>
                Localizer.Instance.Format("ValidationFleetDrive", root.TrimEnd('\\')),
            FleetTargetProblem.ArchivesFolderAlreadyExists =>
                Localizer.Instance.Format("ValidationFleetArchivesExists", Path.Combine(path, "archives")),
            _ => Localizer.Instance["ValidationFleetInvalid"],
        };
    }
}
