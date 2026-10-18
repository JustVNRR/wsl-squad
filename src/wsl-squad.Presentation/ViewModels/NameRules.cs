namespace wsl_squad.Presentation.ViewModels;

internal static class NameRules
{
    public static bool IsValid(string value)
    {
        if (string.IsNullOrEmpty(value) || !char.IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
    }

    public static bool IsValidUser(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] is < 'a' or > 'z')
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '_' or '-');
    }
}
