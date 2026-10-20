using System.Diagnostics;
using Microsoft.Win32;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public sealed class WslRepository : IWslRepository
{
    private const string _lxssPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    private const string _homeScript =
        "user=$(awk -F= '/^[[:space:]]*default[[:space:]]*=/{gsub(/^[[:space:]]+|[[:space:]]+$/, \"\", $2); print $2; exit}' /etc/wsl.conf 2>/dev/null | tr -d '\\r'); " +
        "[ -n \"$user\" ] || user=$(id -un); " +
        "home=$(awk -F: -v u=\"$user\" '$1 == u { print $6; exit }' /etc/passwd | tr -d '\\r'); " +
        "if [ -z \"$home\" ]; then " +
        "other=$(id -un); " +
        "if [ \"$other\" != \"$user\" ]; then " +
        "home=$(awk -F: -v u=\"$other\" '$1 == u { print $6; exit }' /etc/passwd | tr -d '\\r'); " +
        "fi; fi; " +
        "[ -n \"$home\" ] || home=\"${HOME:-}\"; " +
        "[ -n \"$home\" ] || { [ \"$(id -u)\" = 0 ] && home=/root; }; " +
        "if [ -z \"$home\" ] || [ ! -d \"$home/.config/packs\" ]; then " +
        "for candidate in /home/* /root; do " +
        "if [ -d \"$candidate/.config/packs\" ]; then home=\"$candidate\"; break; fi; " +
        "done; fi; " +
        "[ -n \"$home\" ] || { echo 'The instance did not say where its user home is.' >&2; exit 1; }";

    private readonly string _orphanScript;

    private readonly record struct PackLocation(string User, string Home);

    public WslRepository(string distroDir)
    {
        _orphanScript = Path.Combine(distroDir, "packs", "cleanup_orphans.sh");
    }

    public IReadOnlyList<WslInstance> GetAllDistros()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        List<WslInstance> found = [];

        using RegistryKey? root = Registry.CurrentUser.OpenSubKey(_lxssPath);

        if (root is null)
        {
            return found;
        }

        foreach (string keyName in root.GetSubKeyNames())
        {
            using RegistryKey? key = root.OpenSubKey(keyName);
            string? name = key?.GetValue("DistributionName") as string;

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            try
            {
                found.Add(new WslInstance { Name = name });
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning($"[WslRepository] Ignored malformed WSL distribution '{name}': {ex.Message}");
            }
        }

        return found;
    }

    public string GetFolder(string distroName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        using RegistryKey? root = Registry.CurrentUser.OpenSubKey(_lxssPath);

        if (root is null)
        {
            return string.Empty;
        }

        foreach (string keyName in root.GetSubKeyNames())
        {
            using RegistryKey? key = root.OpenSubKey(keyName);
            string? name = key?.GetValue("DistributionName") as string;

            if (name != distroName)
            {
                continue;
            }

            string basePath = key?.GetValue("BasePath") as string ?? string.Empty;

            return basePath.StartsWith(@"\\?\", StringComparison.Ordinal)
                ? basePath[4..].TrimEnd('\\')
                : basePath.TrimEnd('\\');
        }

        return string.Empty;
    }

    public IReadOnlyList<string> GetRunningNames()
    {
        (int _, string output) = NativeCommand.Run("wsl.exe", ["--list", "--quiet", "--running"]);

        return
        [
            .. output
                .Replace("\0", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        ];
    }

    public void Import(string instanceName, string folder, string tarPath, bool interactive = false)
    {
        string[] arguments = ["--import", instanceName, folder, tarPath, "--version", "2"];
        int exitCode = interactive
            ? NativeCommand.RunAttached("wsl.exe", arguments)
            : NativeCommand.Run("wsl.exe", arguments).ExitCode;

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{instanceName}' could not be imported. (Exit code: {exitCode})");
        }
    }

    public void Export(string instanceName, string tarPath, string format)
    {
        (int exitCode, _) = NativeCommand.Run(
            "wsl.exe",
            ["--export", instanceName, tarPath, "--format", format]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{instanceName}' could not be exported. (Exit code: {exitCode})");
        }
    }

    public void Unregister(string instanceName)
    {
        (int exitCode, _) = NativeCommand.Run("wsl.exe", ["--unregister", instanceName]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{instanceName}' could not be unregistered. (Exit code: {exitCode})");
        }
    }

    public void Start(string instanceName)
    {
        NativeCommand.Invoke(
            "wsl.exe",
            ["-d", instanceName, "--exec", "/bin/true"],
            $"'{instanceName}' could not be started.");
    }

    public void Terminate(string instanceName)
    {
        NativeCommand.Invoke(
            "wsl.exe",
            ["--terminate", instanceName],
            $"'{instanceName}' could not be stopped.");
    }

    public void TerminateQuietly(string instanceName)
    {
        _ = NativeCommand.Test("wsl.exe", ["--terminate", instanceName]);
        Thread.Sleep(1000);
    }

    public void Compact(string instanceName)
    {
        NativeCommand.Invoke("wsl.exe", ["--manage", instanceName, "--compact"], "The compact failed.");
    }

    public void OpenProfile(string instanceName)
    {
        Thread.Sleep(400);
        _ = Process.Start(new ProcessStartInfo("wt.exe")
        {
            ArgumentList = { "-p", instanceName },
            UseShellExecute = true,
        });
    }

    public void OpenBareShell(string instanceName)
    {
        _ = Process.Start(new ProcessStartInfo("wsl.exe")
        {
            Arguments = $"-d {instanceName} --cd ~",
            UseShellExecute = true,
        });
    }

    public int RunInInstance(string instanceName, string? asUser, IReadOnlyList<string> arguments, string? workingDirectory)
    {
        List<string> wslArguments = ["-d", instanceName];

        if (!string.IsNullOrEmpty(asUser))
        {
            wslArguments.AddRange(["-u", asUser]);
        }

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            wslArguments.AddRange(["--cd", workingDirectory]);
        }

        wslArguments.Add("--");
        wslArguments.AddRange(arguments);

        return NativeCommand.Run("wsl.exe", wslArguments).ExitCode;
    }

    public IReadOnlyList<string> GetInInstance(string instanceName, IReadOnlyList<string> arguments)
    {
        List<string> wslArguments = ["-d", instanceName, "--"];
        wslArguments.AddRange(arguments);

        (int _, string output) = NativeCommand.Run("wsl.exe", wslArguments, includeError: false);

        return
        [
            .. output
                .Replace("\0", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        ];
    }

    public bool TestInInstance(string instanceName, IReadOnlyList<string> arguments)
    {
        List<string> wslArguments = ["-d", instanceName, "--"];
        wslArguments.AddRange(arguments);

        return NativeCommand.Test("wsl.exe", wslArguments);
    }

    public string GetFamily(string instanceName)
    {
        IReadOnlyList<string> release = GetInInstance(instanceName, ["cat", "/etc/os-release"]);

        if (release.Count == 0)
        {
            return string.Empty;
        }

        Dictionary<string, string> fields = new(StringComparer.Ordinal);

        foreach (string line in release)
        {
            int separator = line.IndexOf('=', StringComparison.Ordinal);

            if (separator <= 0)
            {
                continue;
            }

            string key = line[..separator];

            if (key is "ID" or "ID_LIKE")
            {
                fields[key] = line[(separator + 1)..].Trim('"').ToLowerInvariant();
            }
        }

        if (!fields.TryGetValue("ID", out string? id))
        {
            return string.Empty;
        }

        return id != "debian" &&
               fields.TryGetValue("ID_LIKE", out string? like) &&
               like.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("debian")
            ? "debian"
            : id;
    }

    public void SaveOnboardingScript(string instanceName, string source)
    {
        string throughTheDrives = ToWslPath(source);
        int exitCode = RunInInstance(
            instanceName,
            "root",
            [
                "sh",
                "-c",
                $"cp -f '{throughTheDrives}' /root/onboarding.sh && chmod 755 /root/onboarding.sh && echo /root/onboarding.sh >> /root/.bashrc",
            ],
            string.Empty);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{source}' could not be placed in '{instanceName}' as /root/onboarding.sh - are the Windows drives mounted in it?");
        }
    }

    public void RunOnboarding(string instanceName, string asUser, bool interactive = false)
    {
        string[] arguments = ["-d", instanceName, "-u", "root", "env", "LANG=C", "/root/onboarding.sh", asUser];
        int exitCode = interactive
            ? NativeCommand.RunAttached("wsl.exe", arguments)
            : NativeCommand.Run("wsl.exe", arguments).ExitCode;

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"The onboarding.sh configuration script failed. (Exit code: {exitCode})");
        }
    }

    public IReadOnlyList<string> GetInstalledPacks(string instanceName)
    {
        string script = _homeScript +
            "; for folder in \"$home\" /home/* /root; do " +
            "find \"$folder/.config/packs\" -mindepth 2 -maxdepth 2 -name pack.conf 2>/dev/null; " +
            "done | sort -u";

        (int exitCode, string output) = NativeCommand.RunWithInput(
            "wsl.exe",
            ["-d", instanceName, "--", "sh", "-s"],
            script,
            includeError: true);

        if (exitCode != 0)
        {
            return [];
        }

        IReadOnlyList<string> found =
        [
            .. output
                .Replace("\0", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        ];

        return ParseInstalledPacks(found);
    }

    private PackLocation ResolveLocation(string instanceName)
    {
        string script = _homeScript +
            "; printf 'WSL_SQUAD_USER=%s\\n' \"$user\"; " +
            "printf 'WSL_SQUAD_HOME=%s\\n' \"$home\"";

        (int exitCode, string output) = NativeCommand.RunWithInput(
            "wsl.exe",
            ["-d", instanceName, "--", "sh", "-s"],
            script,
            includeError: true);

        string user = Marker(output, "WSL_SQUAD_USER=");
        string home = Marker(output, "WSL_SQUAD_HOME=");

        if (exitCode != 0 || home.Length == 0)
        {
            throw new InvalidOperationException($"'{instanceName}' did not say where its user's home is.");
        }

        return new PackLocation(user, home);
    }

    private static string Marker(string output, string prefix)
    {
        foreach (string line in output.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[prefix.Length..].Trim();
            }
        }

        return string.Empty;
    }

    internal static IReadOnlyList<string> ParseInstalledPacks(IReadOnlyList<string> found)
    {
        const string suffix = "/pack.conf";

        return
        [
            .. found
                .Where(line => line.EndsWith(suffix, StringComparison.Ordinal))
                .Select(line => line[..^suffix.Length])
                .Select(folder => folder[(folder.LastIndexOf('/') + 1)..])
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }

    internal static string ShellValue(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }

    private static string PackFolderPath(PackLocation location, string packName)
    {
        if (string.IsNullOrEmpty(packName))
        {
            throw new ArgumentException("A pack name is required.", nameof(packName));
        }

        return $"{location.Home}/.config/packs/{packName}";
    }

    public int CopyPackIntoInstance(string instanceName, string sourceFolder, string packName)
    {
        PackLocation location = ResolveLocation(instanceName);
        string target = PackFolderPath(location, packName);
        int code = RunInInstance(instanceName, string.Empty, ["mkdir", "-p", target], string.Empty);

        if (code != 0)
        {
            return code;
        }

        string throughTheDrives = ToWslPath(sourceFolder);
        bool drivesMounted = RunInInstance(
            instanceName,
            string.Empty,
            ["test", "-d", throughTheDrives],
            string.Empty) == 0;

        bool copied;

        if (drivesMounted)
        {
            copied = RunInInstance(
                instanceName,
                string.Empty,
                ["cp", "-r", ".", $"{target}/"],
                sourceFolder) == 0;
        }
        else
        {
            try
            {
                string unc = $@"\\wsl.localhost\{instanceName}" + target.Replace('/', '\\');
                CopyTree(sourceFolder, unc);
                copied = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                copied = false;
            }
        }

        if (copied)
        {
            _ = RunInInstance(
                instanceName,
                string.Empty,
                ["find", target, "-name", "*.sh", "-exec", "chmod", "+x", "{}", "+"],
                string.Empty);

            return 0;
        }

        _ = RunInInstance(instanceName, string.Empty, ["rm", "-rf", target], string.Empty);

        return 1;
    }

    public int CopyPackFileIntoInstance(string instanceName, string sourceFile, string packName, string fileName)
    {
        PackLocation location = ResolveLocation(instanceName);
        string target = $"{PackFolderPath(location, packName)}/{fileName}";
        string throughTheDrives = ToWslPath(sourceFile);
        bool drivesMounted = RunInInstance(
            instanceName,
            string.Empty,
            ["test", "-f", throughTheDrives],
            string.Empty) == 0;

        if (drivesMounted)
        {
            return RunInInstance(
                instanceName,
                string.Empty,
                ["cp", "-f", throughTheDrives, target],
                string.Empty);
        }

        try
        {
            string unc = $@"\\wsl.localhost\{instanceName}" + target.Replace('/', '\\');
            File.Copy(sourceFile, unc, overwrite: true);

            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 1;
        }
    }

    public int RunScriptInInstance(
        string instanceName,
        string workingDirectory,
        string script,
        bool asRoot,
        string? errorLog,
        bool interactive = false)
    {
        List<string> wslArguments = ["-d", instanceName];

        if (asRoot)
        {
            PackLocation location = ResolveLocation(instanceName);
            wslArguments.AddRange(
                ["-u", "root", "--cd", workingDirectory, "--", "env", "LANG=C", $"HOME={location.Home}", "bash", script]);
        }
        else
        {
            wslArguments.AddRange(["--cd", workingDirectory, "--", "env", "LANG=C", "bash", script]);
        }

        if (interactive)
        {
            return NativeCommand.RunAttached("wsl.exe", wslArguments);
        }

        (int code, string output) = NativeCommand.Run("wsl.exe", wslArguments);

        TryAppendErrorLog(errorLog, output);

        return code;
    }

    public int RunPackScript(
        string instanceName,
        string packName,
        string script,
        bool asRoot,
        string? errorLog,
        bool interactive = false)
    {
        PackLocation location = ResolveLocation(instanceName);
        string target = PackFolderPath(location, packName);
        List<string> wslArguments = ["-d", instanceName];

        if (asRoot)
        {
            wslArguments.AddRange(["-u", "root"]);
        }

        wslArguments.AddRange(
            ["--cd", target, "--", "env", "LANG=C", $"HOME={location.Home}", "bash", script]);

        if (interactive)
        {
            return NativeCommand.RunAttached("wsl.exe", wslArguments);
        }

        (int code, string output) = NativeCommand.Run("wsl.exe", wslArguments);

        TryAppendErrorLog(errorLog, output);

        return code;
    }

    public bool PackHasFile(string instanceName, string packName, string fileName)
    {
        PackLocation location = ResolveLocation(instanceName);
        string target = $"{PackFolderPath(location, packName)}/{fileName}";

        return RunInInstance(instanceName, string.Empty, ["test", "-f", target], string.Empty) == 0;
    }

    public int RemovePackFromInstance(string instanceName, string packName)
    {
        PackLocation location = ResolveLocation(instanceName);

        return RunInInstance(
            instanceName,
            string.Empty,
            ["rm", "-rf", PackFolderPath(location, packName)],
            string.Empty);
    }

    public bool OpenPackSudoWindow(string instanceName)
    {
        PackLocation location = ResolveLocation(instanceName);

        if (location.User.Length == 0)
        {
            return false;
        }

        string script = "printf '%s\\n' " + ShellValue(location.User + " ALL=(ALL) NOPASSWD: ALL") +
            " > /tmp/wsl-squad-pack-sudo" +
            " && chmod 0440 /tmp/wsl-squad-pack-sudo" +
            " && visudo -cf /tmp/wsl-squad-pack-sudo > /dev/null" +
            " && mv /tmp/wsl-squad-pack-sudo /etc/sudoers.d/90-wsl-squad-packs";

        (int code, _) = NativeCommand.RunWithInput(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "sh", "-s"],
            script,
            includeError: false);

        return code == 0;
    }

    public void ClosePackSudoWindow(string instanceName)
    {
        _ = RunInInstance(
            instanceName,
            "root",
            ["rm", "-f", "/etc/sudoers.d/90-wsl-squad-packs"],
            string.Empty);
    }

    public bool RunOrphanCleanup(string instanceName)
    {
        if (!File.Exists(_orphanScript))
        {
            return false;
        }

        string remoteScript = "/tmp/cleanup_orphans.sh";
        int sent = RunInInstance(
            instanceName,
            string.Empty,
            ["cp", "cleanup_orphans.sh", remoteScript],
            Path.GetDirectoryName(_orphanScript) ?? string.Empty);

        if (sent != 0)
        {
            return false;
        }

        int cleaned = RunScriptInInstance(instanceName, "/tmp", "cleanup_orphans.sh", asRoot: true, errorLog: string.Empty);
        _ = RunInInstance(instanceName, string.Empty, ["rm", "-f", remoteScript], string.Empty);

        return cleaned == 0;
    }

    private static string ToWslPath(string source)
    {
        string path = source.Replace('\\', '/');

        return "/mnt/" + char.ToLowerInvariant(path[0]) + path[2..];
    }

    private static void CopyTree(string source, string target)
    {
        _ = Directory.CreateDirectory(target);

        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    private static void TryAppendErrorLog(string? errorLog, string content)
    {
        if (string.IsNullOrEmpty(errorLog) || content.Length == 0)
        {
            return;
        }

        try
        {
            File.AppendAllText(errorLog, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[WslRepository] Could not append script output to '{errorLog}': {ex.Message}");
        }
    }
}
