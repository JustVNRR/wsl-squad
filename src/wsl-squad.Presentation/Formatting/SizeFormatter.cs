using System.Globalization;

namespace wsl_squad.Presentation.Formatting;

/// <summary>Formats byte counts with binary units.</summary>
public static class SizeFormatter
{
    private static readonly string[] _units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Format(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);

        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < _units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{bytes} B")
            : string.Create(CultureInfo.InvariantCulture, $"{value:0.0} {_units[unitIndex]}");
    }
}
