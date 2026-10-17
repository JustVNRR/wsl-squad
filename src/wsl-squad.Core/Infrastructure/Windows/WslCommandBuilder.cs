using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Builds exact command lines for <c>wsl.exe</c>.</summary>
public static class WslCommandBuilder
{
    private static readonly IReadOnlyDictionary<string, string> _utf8Environment =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["WSL_UTF8"] = "1" };

    public static ProcessInvocation ListRunning()
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["--list", "--running", "--quiet"],
            environment: _utf8Environment);
    }
}
