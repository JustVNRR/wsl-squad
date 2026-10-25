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

}
