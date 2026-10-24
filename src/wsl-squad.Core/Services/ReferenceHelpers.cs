using System.Globalization;

namespace WslSquad.Core.Services;

public enum FleetTargetProblem
{
    None,
    Empty,
    NotAbsolutePath,
    DriveUnavailable,
    ArchivesFolderAlreadyExists,
}

public static class ReferenceHelpers
{
    public static string FormatSize(long bytes)
    {
        const double kilo = 1024d;
        const double mega = kilo * 1024;
        const double giga = mega * 1024;

        if (bytes >= giga)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{bytes / giga:N1} GB");
        }

        if (bytes >= mega)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{bytes / mega:N0} MB");
        }

        return string.Create(CultureInfo.CurrentCulture, $"{bytes / kilo:N0} KB");
    }

    public static FleetTargetProblem TestFleetTarget(string? target)
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
