using System.Text.RegularExpressions;

namespace wsl_squad.Core.Infrastructure.Storage;

/// <summary>Path helpers that treat both Windows and Unix separators consistently.</summary>
public static partial class PathHelper
{
    public static bool IsFullyQualified(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathFullyQualified(path))
        {
            return true;
        }

        return DriveLetterPattern().IsMatch(path) || path.StartsWith(@"\\", StringComparison.Ordinal);
    }

    public static string? GetParent(string path)
    {
        string normalized = NormalizeSeparators(path).TrimEnd('/');
        int index = normalized.LastIndexOf('/');

        return index <= 0 ? null : normalized[..index];
    }

    public static bool AreSamePath(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(
            NormalizeForComparison(left),
            NormalizeForComparison(right),
            StringComparison.Ordinal);
    }

    public static bool IsUnder(string candidate, string root)
    {
        string normalizedCandidate = NormalizeForComparison(candidate);
        string normalizedRoot = NormalizeForComparison(root);

        return normalizedCandidate == normalizedRoot ||
               normalizedCandidate.StartsWith(normalizedRoot + "/", StringComparison.Ordinal);
    }

    public static string NormalizeForComparison(string path)
    {
        return NormalizeSeparators(path).TrimEnd('/').ToLowerInvariant();
    }

    public static string NormalizeSeparators(string path)
    {
        return path.Replace('\\', '/');
    }

    public static string NormalizeRegistryBasePath(string basePath)
    {
        string normalized = basePath.StartsWith(@"\\?\", StringComparison.Ordinal)
            ? basePath[4..]
            : basePath;

        return normalized.TrimEnd('\\', '/');
    }

    [GeneratedRegex("^[A-Za-z]:[\\\\/]", RegexOptions.CultureInvariant)]
    private static partial Regex DriveLetterPattern();
}
