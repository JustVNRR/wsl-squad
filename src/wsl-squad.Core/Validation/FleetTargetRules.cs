namespace WslSquad.Core.Validation;

public enum FleetTargetProblem
{
    None,
    Empty,
    NotAbsolutePath,
    DriveUnavailable,
    ArchivesFolderAlreadyExists,
}

public static class FleetTargetRules
{
    public static FleetTargetProblem Test(string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return FleetTargetProblem.Empty;
        }

        string? root = Path.GetPathRoot(target);

        if (string.IsNullOrEmpty(root) ||
            !Path.IsPathRooted(target) ||
            (root.Length == 2 && root[1] == ':'))
        {
            return FleetTargetProblem.NotAbsolutePath;
        }

        if (!Directory.Exists(root))
        {
            return FleetTargetProblem.DriveUnavailable;
        }

        string archives = Path.Combine(target, "archives");

        return Directory.Exists(archives)
            ? FleetTargetProblem.ArchivesFolderAlreadyExists
            : FleetTargetProblem.None;
    }
}
