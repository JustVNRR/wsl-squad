using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Builds the <c>wsl.exe</c> command lines used to manage packs.</summary>
public static class PackCommandBuilder
{
    public const string SudoersPath = "/etc/sudoers.d/90-wsl-squad-packs";

    private static readonly IReadOnlyDictionary<string, string> _utf8Environment =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["WSL_UTF8"] = "1" };

    public static ProcessInvocation InstalledPacks(string instanceName)
    {
        const string Script =
            "find \"$HOME/.config/packs\" -mindepth 2 -maxdepth 2 -name pack.conf -printf '%h\\n' 2>/dev/null || true";

        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "--", "bash", "-lc", Script],
            environment: _utf8Environment);
    }

    public static ProcessInvocation OsRelease(string instanceName)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "--", "cat", "/etc/os-release"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation UserHome(string instanceName, string user)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", user, "--", "printenv", "HOME"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation CopyPack(
        string instanceName,
        string user,
        string wslSourceDirectory,
        string packName)
    {
        string target = $"$HOME/.config/packs/{packName}";
        string script =
            $"mkdir -p \"{target}\" && cp -r {Quote(wslSourceDirectory)}/. \"{target}/\" && " +
            $"find \"{target}\" -name '*.sh' -exec chmod +x {{}} +";

        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", user, "--", "bash", "-lc", script],
            environment: _utf8Environment);
    }

    public static ProcessInvocation RunScript(
        string instanceName,
        string user,
        string? homeDirectory,
        string linuxScriptPath,
        IReadOnlyList<string> arguments)
    {
        List<string> invocationArguments = ["-d", instanceName, "-u", user, "--", "env", "LANG=C"];

        if (!string.IsNullOrWhiteSpace(homeDirectory))
        {
            invocationArguments.Add($"HOME={homeDirectory}");
        }

        invocationArguments.Add("/bin/bash");
        invocationArguments.Add(linuxScriptPath);
        invocationArguments.AddRange(arguments);

        return new ProcessInvocation("wsl.exe", invocationArguments, environment: _utf8Environment);
    }

    public static ProcessInvocation OpenSudo(string instanceName, string user)
    {
        string rule = $"{user} ALL=(ALL) NOPASSWD:ALL";
        string script =
            $"printf '%s\\n' {Quote(rule)} > {SudoersPath} && chmod 0440 {SudoersPath} && visudo -cf {SudoersPath}";

        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "bash", "-lc", script],
            environment: _utf8Environment);
    }

    public static ProcessInvocation CloseSudo(string instanceName)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "rm", "-f", SudoersPath],
            environment: _utf8Environment);
    }

    public static ProcessInvocation RemovePackFolder(string instanceName, string linuxHome, string packName)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "rm", "-rf", $"{linuxHome}/.config/packs/{packName}"],
            environment: _utf8Environment);
    }

    public static ProcessInvocation CleanupOrphans(string instanceName, string linuxScriptPath)
    {
        return new ProcessInvocation(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--cd", "/tmp", "--", "env", "LANG=C", "/bin/bash", linuxScriptPath],
            environment: _utf8Environment);
    }

    public static string Quote(string value)
    {
        return $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";
    }
}
