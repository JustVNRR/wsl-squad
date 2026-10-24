using System.Globalization;
using System.Text;

namespace WslSquad.Core.Validation;

public enum NameProblem
{
    None,
    Empty,
    InvalidFormat,
    AlreadyTaken,
}

public static class NameRules
{
    private const int _maxLinuxUserLength = 32;

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || !char.IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    public static NameProblem ValidateInstanceName(string? name, IEnumerable<string> takenNames)
    {
        ArgumentNullException.ThrowIfNull(takenNames);

        if (string.IsNullOrWhiteSpace(name))
        {
            return NameProblem.Empty;
        }

        if (!IsValid(name))
        {
            return NameProblem.InvalidFormat;
        }

        return takenNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? NameProblem.AlreadyTaken
            : NameProblem.None;
    }

    public static bool IsValidUser(string? value)
    {
        if (string.IsNullOrEmpty(value)
            || value.Length > _maxLinuxUserLength
            || !char.IsAsciiLetterLower(value[0]))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!char.IsAsciiLetterLower(character) &&
                !char.IsAsciiDigit(character) &&
                character is not ('_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    public static string ProposeUserName(string? rawName = null)
    {
        string source = (rawName ?? Environment.UserName)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        StringBuilder clean = new(source.Length);

        foreach (char character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterLower(character) ||
                (clean.Length > 0 && (char.IsAsciiDigit(character) || character is '_' or '-')))
            {
                _ = clean.Append(character);
            }
            else if (clean.Length > 0 && clean[^1] != '-')
            {
                _ = clean.Append('-');
            }
        }

        string proposal = clean.ToString().TrimEnd('-', '_');

        if (proposal.Length > _maxLinuxUserLength)
        {
            proposal = proposal[.._maxLinuxUserLength].TrimEnd('-', '_');
        }

        return IsValidUser(proposal) ? proposal : string.Empty;
    }
}
