using System.Text.RegularExpressions;

namespace wsl_squad.Core.Infrastructure.Docker;

/// <summary>Builds Docker-safe names from instance names.</summary>
public static partial class DockerTag
{
    public static string Slugify(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string slug = InvalidCharacterPattern().Replace(name.ToLowerInvariant(), "-");
        slug = slug.Trim('-', '.', '_');

        return slug.Length == 0 ? "instance" : slug;
    }

    [GeneratedRegex("[^a-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidCharacterPattern();
}
