namespace WslSquad.UI.Cli;

public sealed record CliInvocation(string? Command, string? Name)
{
    public static CliInvocation Parse(IReadOnlyList<string> args)
    {
        string? command = args.Count > 0 ? args[0] : null;
        string? name = args.Count > 1 ? args[1] : null;

        return new CliInvocation(command, name);
    }
}
