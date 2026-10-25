using System.Globalization;

namespace WslSquad.Core.Formatting;

public static class SizeFormatter
{
    public static string Format(long bytes)
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
}
