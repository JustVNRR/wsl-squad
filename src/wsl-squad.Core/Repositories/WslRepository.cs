using System.Diagnostics;
using Microsoft.Win32;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public sealed class WslRepository : IWslRepository
{
    private const string _lxssPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    private readonly string _orphanScript;

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

        (int _, string output) = NativeCommand.Run("wsl.exe", wslArguments);

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

    public string GetHome(string instanceName)
    {
        IReadOnlyList<string> values = GetInInstance(instanceName, ["printenv", "HOME"]);

        return values.Count > 0 ? values[0] : string.Empty;
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

    public IReadOnlyList<string> GetInstalledPacks(string instanceName, string packsDirectory)
    {
        IReadOnlyList<string> found = GetInInstance(
            instanceName,
            ["find", packsDirectory, "-mindepth", "2", "-maxdepth", "2", "-name", "pack.conf"]);

        return
        [
            .. found
                .Select(line => line.Replace("/pack.conf", string.Empty, StringComparison.Ordinal))
                .Select(line => line.Split('/')[^1])
                .Order(StringComparer.Ordinal),
        ];
    }

    public int CopyFolderIntoInstance(string instanceName, string sourceFolder, string target)
    {
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
            copied = RunInInstance(instanceName, string.Empty, ["cp", "-r", ".", $"{target}/"], sourceFolder) == 0;
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
                ["sh", "-c", $"find '{target}' -name '*.sh' -exec chmod +x {{}} +"],
                string.Empty);

            return 0;
        }

        _ = RemoveFolderInInstance(instanceName, target);

        return 1;
    }

    public int CopyFileIntoInstance(string instanceName, string sourceFile, string targetPath)
    {
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
                ["cp", "-f", throughTheDrives, targetPath],
                string.Empty);
        }

        try
        {
            string unc = $@"\\wsl.localhost\{instanceName}" + targetPath.Replace('/', '\\');
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
            string userHome = GetHome(instanceName);
            wslArguments.AddRange(["-u", "root", "--cd", workingDirectory, "--", "env", "LANG=C", $"HOME={userHome}", "bash", script]);
        }
        else
        {
            wslArguments.AddRange(["--cd", workingDirectory, "--", "env", "LANG=C", "bash", script]);
        }

        if (interactive)
        {
            return RunScriptInteractive(wslArguments, errorLog);
        }

        (int code, string output) = NativeCommand.Run("wsl.exe", wslArguments);

        TryAppendErrorLog(errorLog, output);

        return code;
    }

    private static int RunScriptInteractive(IReadOnlyList<string> arguments, string? errorLog)
    {
        ProcessStartInfo start = new()
        {
            FileName = "wsl.exe",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new InvalidOperationException("'wsl.exe' could not be started.");
        System.Text.StringBuilder captured = new();

        void Keep(string? line, bool error)
        {
            if (line is null)
            {
                return;
            }

            lock (captured)
            {
                _ = captured.AppendLine(line);
            }

            if (error)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        process.OutputDataReceived += (_, e) => Keep(e.Data, error: false);
        process.ErrorDataReceived += (_, e) => Keep(e.Data, error: true);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(errorLog))
        {
            string output;

            lock (captured)
            {
                output = captured.ToString();
            }

            TryAppendErrorLog(errorLog, output);
        }

        return process.ExitCode;
    }

    public bool HasInInstance(string instanceName, string path)
    {
        return RunInInstance(instanceName, string.Empty, ["test", "-f", path], string.Empty) == 0;
    }

    public int RemoveFolderInInstance(string instanceName, string target)
    {
        return RunInInstance(instanceName, string.Empty, ["rm", "-rf", target], string.Empty);
    }

    public bool OpenPackSudoWindow(string instanceName)
    {
        string user = Path.GetFileName(GetHome(instanceName));

        if (user.Length == 0)
        {
            return false;
        }

        string command = "printf '%s\\n' '" + user + " ALL=(ALL) NOPASSWD: ALL' > /tmp/wsl-fleet-pack-sudo" +
            " && chmod 0440 /tmp/wsl-fleet-pack-sudo" +
            " && visudo -cf /tmp/wsl-fleet-pack-sudo > /dev/null" +
            " && mv /tmp/wsl-fleet-pack-sudo /etc/sudoers.d/90-wsl-fleet-packs";

        return RunInInstance(instanceName, "root", ["bash", "-c", command], string.Empty) == 0;
    }

    public void ClosePackSudoWindow(string instanceName)
    {
        _ = RunInInstance(
            instanceName,
            "root",
            ["rm", "-f", "/etc/sudoers.d/90-wsl-fleet-packs"],
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
