using System.Text.RegularExpressions;

using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Core.Infrastructure.Packs;

/// <summary>Parses GNU-make style <c>pack.conf</c> files.</summary>
public static partial class PackConfParser
{
    private const string _defaultFamily = "debian";

    public static PackConf Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Dictionary<string, string> values = new(StringComparer.Ordinal);

        foreach (string rawLine in content.Split('\n'))
        {
            string line = StripComment(rawLine.TrimEnd('\r')).Trim();

            if (line.Length == 0)
            {
                continue;
            }

            Match match = AssignmentPattern().Match(line);

            if (match.Success)
            {
                values[match.Groups["key"].Value] = match.Groups["value"].Value.Trim();
            }
        }

        string family = GetValue(values, "PACK_FAMILY");
        string visible = GetValue(values, "PACK_VISIBLE");

        return new PackConf(
            GetValue(values, "PACK_DESCRIPTION"),
            family.Length == 0 ? _defaultFamily : family,
            ParseList(GetValue(values, "PACK_REQUIRES")),
            !string.Equals(visible, "no", StringComparison.OrdinalIgnoreCase),
            GetValue(values, "PACK_WELCOME"));
    }

    private static string StripComment(string line)
    {
        int commentIndex = line.IndexOf('#', StringComparison.Ordinal);

        return commentIndex < 0 ? line : line[..commentIndex];
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.GetValueOrDefault(key, string.Empty);
    }

    private static IReadOnlyList<string> ParseList(string value)
    {
        return [.. value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    [GeneratedRegex(
        @"^\s*(?<key>PACK_[A-Z0-9_]+)\s*(?::=|\?=|\+=|=)\s*(?<value>.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentPattern();
}
