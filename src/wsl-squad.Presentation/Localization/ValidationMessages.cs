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
            NameProblem.Empty => "Give a name.",
            NameProblem.InvalidFormat => "Letters, digits, '.', '_' and '-' only.",
            NameProblem.AlreadyTaken => $"An instance named '{attemptedName}' already exists.",
            _ => "Invalid name.",
        };
    }

    public static string? ToMessage(this FleetTargetProblem problem, string? target = null)
    {
        string path = target ?? string.Empty;
        string root = Path.GetPathRoot(path) ?? string.Empty;

        return problem switch
        {
            FleetTargetProblem.None => null,
            FleetTargetProblem.Empty => "no destination was given.",
            FleetTargetProblem.NotAbsolutePath =>
                $"'{path}' is not an absolute folder - give a full path, like 'D:\\WSL'.",
            FleetTargetProblem.DriveUnavailable =>
                $"the drive '{root.TrimEnd('\\')}' does not answer.",
            FleetTargetProblem.ArchivesFolderAlreadyExists =>
                $"'{Path.Combine(path, "archives")}' already exists - move it away first.",
            _ => "Invalid fleet target.",
        };
    }
}
