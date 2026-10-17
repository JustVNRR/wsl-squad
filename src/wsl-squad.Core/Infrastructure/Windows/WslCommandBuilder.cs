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

    public static ProcessInvocation Import(string name, string installPath, string tarPath)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["--import", name, installPath, tarPath, "--version", "2"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation Unregister(string name)
    {
        return new ProcessInvocation("wsl.exe", ["--unregister", name], environment: _utf8Environment);
    }

    public static ProcessInvocation Terminate(string name)
    {
        return new ProcessInvocation("wsl.exe", ["--terminate", name], environment: _utf8Environment);
    }

    public static ProcessInvocation CopyFile(string distribution, string wslSourcePath, string linuxTargetPath)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", distribution, "-u", "root", "--", "cp", wslSourcePath, linuxTargetPath],
            environment: _utf8Environment);
    }

    public static ProcessInvocation Export(string name, string tarPath, string format)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["--export", name, tarPath, "--format", format],
            environment: _utf8Environment);
    }

    public static ProcessInvocation Compact(string name)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["--manage", name, "--compact"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation Start(string name)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", name, "--exec", "/bin/true"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation RunOnboarding(string distribution, string linuxScriptPath, string? user)
    {
        List<string> arguments = ["-d", distribution, "-u", "root", "--", "env", "LANG=C", "/bin/bash", linuxScriptPath];

        if (!string.IsNullOrWhiteSpace(user))
        {
            arguments.Add(user);
        }

        return new ProcessInvocation("wsl.exe", arguments, environment: _utf8Environment);
    }
}
