using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation;

public static class ValidationMessages
{
    public static string? ToMessage(this NameProblem problem, string? attemptedName = null)
    {
        return problem switch
        {
            NameProblem.None => null,
            NameProblem.Empty => "Give a name.",
            NameProblem.InvalidFormat => "Letters, digits, '.', '_' and '-' only.",
            NameProblem.AlreadyTaken => $"An instance named '{attemptedName}' already exists.",
            _ => "Invalid name.",
        };
    }
}
