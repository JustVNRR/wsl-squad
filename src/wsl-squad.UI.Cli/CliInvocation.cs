namespace WslSquad.UI.Cli;

public sealed record CliInvocation(string? Command, string? Name, string? BuildRequest)
{
    public static CliInvocation Parse(IReadOnlyList<string> args)
    {
        string? command = args.Count > 0 ? args[0] : null;
        string? name = args.Count > 1 ? args[1] : null;

        if (command != "build")
        {
            if (args.Skip(1).Contains("--request", StringComparer.Ordinal))
            {
                throw new ArgumentException("--request is only accepted by the build command.");
            }

            return new CliInvocation(command, name, null);
        }

        int requestAt = -1;

        for (int index = 1; index < args.Count; index++)
        {
            if (string.Equals(args[index], "--request", StringComparison.Ordinal))
            {
                requestAt = index;
                break;
            }
        }

        if (requestAt < 0)
        {
            return new CliInvocation(command, name, null);
        }

        if (requestAt + 1 >= args.Count || string.IsNullOrWhiteSpace(args[requestAt + 1]))
        {
            throw new ArgumentException("--request needs the path of a build request file.");
        }

        return new CliInvocation(command, null, args[requestAt + 1]);
    }
}
