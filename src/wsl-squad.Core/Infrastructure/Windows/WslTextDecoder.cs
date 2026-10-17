namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Normalizes <c>wsl.exe</c> list output across encodings.</summary>
public static class WslTextDecoder
{
    public static IReadOnlyList<string> ParseDistributionList(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return [];
        }

        List<string> names = [];

        foreach (string line in output.Replace("\0", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            string name = line.Trim().TrimStart('\uFEFF');
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
    }
}
