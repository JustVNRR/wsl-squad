using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Builds command lines that open a shell for an instance.</summary>
public static class TerminalCommandBuilder
{
    private static readonly IReadOnlyDictionary<string, string> _utf8Environment =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["WSL_UTF8"] = "1" };

    public static ProcessInvocation Profile(string instanceName)
    {
        return new ProcessInvocation("wt.exe", ["-p", instanceName]);
    }

    public static ProcessInvocation Shell(string instanceName)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "--cd", "~"],
            environment: _utf8Environment);
    }
}
