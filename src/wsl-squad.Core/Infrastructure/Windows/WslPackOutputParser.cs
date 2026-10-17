using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Parses the textual output of the in-distribution pack commands.</summary>
public static class WslPackOutputParser
{
    public static IReadOnlyList<string> ParseInstalledPackNames(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        List<string> names = [];

        foreach (string line in output.Replace("\0", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            string directory = line.Trim();

            if (directory.Length == 0)
            {
                continue;
            }

            string normalized = PathHelper.NormalizeSeparators(directory).TrimEnd('/');
            int index = normalized.LastIndexOf('/');
            string name = index < 0 ? normalized : normalized[(index + 1)..];

            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);

        return names;
    }

    public static IReadOnlyList<string> ParseFamilyTokens(string? osRelease)
    {
        if (string.IsNullOrWhiteSpace(osRelease))
        {
            return [];
        }

        List<string> tokens = [];

        foreach (string line in osRelease.Replace("\0", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("ID=", StringComparison.Ordinal))
            {
                AddTokens(tokens, trimmed[3..]);
            }
            else if (trimmed.StartsWith("ID_LIKE=", StringComparison.Ordinal))
            {
                AddTokens(tokens, trimmed[8..]);
            }
        }

        return [.. tokens.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static void AddTokens(List<string> tokens, string value)
    {
        string cleaned = value.Trim().Trim('"', '\'');

        foreach (string token in cleaned.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            tokens.Add(token.ToLowerInvariant());
        }
    }
}
