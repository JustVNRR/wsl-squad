using System.Globalization;

namespace wsl_squad.Core.Services;

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

    public static string? TestFleetTarget(string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return "no destination was given.";
        }

        string? root = Path.GetPathRoot(target);

        if (string.IsNullOrEmpty(root) ||
            !Path.IsPathRooted(target) ||
            (root.Length == 2 && root[1] == ':'))
        {
            return $"'{target}' is not an absolute folder - give a full path, like 'D:\\WSL'.";
        }

        if (!Directory.Exists(root))
        {
            return $"the drive '{root.TrimEnd('\\')}' does not answer.";
        }

        string archives = Path.Combine(target, "archives");

        return Directory.Exists(archives)
            ? $"'{archives}' already exists - move it away first."
            : null;
    }
}
