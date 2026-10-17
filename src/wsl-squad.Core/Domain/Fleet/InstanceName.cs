using System.Text.RegularExpressions;

namespace wsl_squad.Core.Domain.Fleet;

/// <summary>Validation rules for instance, archive and pack names.</summary>
public static partial class InstanceName
{
    public static bool IsValid(string? name)
    {
        return name is { Length: > 0 } && Pattern().IsMatch(name);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
