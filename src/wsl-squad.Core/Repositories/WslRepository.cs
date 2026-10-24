using WslSquad.Core.Exceptions;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public sealed class WslRepository : IWslRepository
{
    private const string _lxssPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

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
            throw new WslSquadException(
                WslSquadErrorCode.WslImportFailed,
                instanceName,
                $"exit code {exitCode}");
        }
    }

    public void Export(string instanceName, string tarPath, string format)
    {
        (int exitCode, _) = NativeCommand.Run(
            "wsl.exe",
            ["--export", instanceName, tarPath, "--format", format]);

        if (exitCode != 0)
        {
            throw new WslSquadException(
                WslSquadErrorCode.WslExportFailed,
                instanceName,
                $"exit code {exitCode}");
        }
    }

    public void Unregister(string instanceName)
    {
        (int exitCode, _) = NativeCommand.Run("wsl.exe", ["--unregister", instanceName]);

        if (exitCode != 0)
        {
            throw new WslSquadException(
                WslSquadErrorCode.WslUnregisterFailed,
                instanceName,
                $"exit code {exitCode}");
        }
    }

    public void Start(string instanceName)
    {
        NativeCommand.Invoke(
            "wsl.exe",
            ["-d", instanceName, "--exec", "/bin/true"],
            WslSquadErrorCode.WslStartFailed,
            instanceName);
    }

    public void Terminate(string instanceName)
    {
        NativeCommand.Invoke(
            "wsl.exe",
            ["--terminate", instanceName],
            WslSquadErrorCode.WslStopFailed,
            instanceName);
    }

    public void TerminateQuietly(string instanceName)
    {
        _ = NativeCommand.Test("wsl.exe", ["--terminate", instanceName]);
        Thread.Sleep(1000);
    }

    public void Compact(string instanceName)
    {
        NativeCommand.Invoke(
            "wsl.exe",
            ["--manage", instanceName, "--compact"],
            WslSquadErrorCode.WslCompactFailed,
            instanceName);
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

    public void OpenBareShellAsRoot(string instanceName)
    {
        _ = Process.Start(new ProcessStartInfo("wsl.exe")
        {
            Arguments = $"-d {instanceName} -u root --cd ~",
            UseShellExecute = true,
        });
    }

    public void SavePackManager(
        string instanceName,
        string script,
        string cleanupScript,
        string bootstrapScript,
        string catalogPath)
    {
        string normalized = NormalizeShell(script);
        string cleanup = NormalizeShell(cleanupScript);
        string bootstrap = NormalizeShell(bootstrapScript);
        string catalog = Quote(ToWslPath(catalogPath));
        string setup =
            "cat > /root/manage-packs.sh <<'WslSquad_PACK_EOF'\n" +
            normalized + "\nWslSquad_PACK_EOF\n" +
            "cat > /root/manage-packs-cleanup.sh <<'WslSquad_CLEANUP_EOF'\n" +
            cleanup + "\nWslSquad_CLEANUP_EOF\n" +
            "cat > /root/manage-packs-bootstrap.sh <<'WslSquad_BOOTSTRAP_EOF'\n" +
            bootstrap + "\nWslSquad_BOOTSTRAP_EOF\n" +
            "chmod 755 /root/manage-packs.sh /root/manage-packs-cleanup.sh /root/manage-packs-bootstrap.sh || exit 1\n" +
            "printf '%s' " + catalog + " > /root/manage-packs.catalog\n";

        (int exitCode, _) = NativeCommand.RunWithInput(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "sh", "-s"],
            setup,
            includeError: true);

        if (exitCode != 0)
        {
            throw new WslSquadException(
                WslSquadErrorCode.WslPackManagerCopyFailed,
                instanceName);
        }
    }

    public void OpenPackManager(string instanceName, bool useProfile)
    {
        ProcessStartInfo start = new()
        {
            FileName = "wt.exe",
            UseShellExecute = true,
            WorkingDirectory = Path.GetTempPath(),
        };

        start.ArgumentList.Add("wsl.exe");
        start.ArgumentList.Add("-d");
        start.ArgumentList.Add(instanceName);
        start.ArgumentList.Add("-u");
        start.ArgumentList.Add("root");
        start.ArgumentList.Add("--cd");
        start.ArgumentList.Add("/root");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("/bin/sh");
        start.ArgumentList.Add("/root/manage-packs-bootstrap.sh");

        if (useProfile)
        {
            start.ArgumentList.Insert(0, instanceName);
            start.ArgumentList.Insert(0, "-p");
            Thread.Sleep(400);
        }

        try
        {
            if (Process.Start(start) is not null)
            {
                return;
            }

            Trace.TraceWarning("[WslRepository] Windows Terminal did not start.");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            Trace.TraceWarning($"[WslRepository] Windows Terminal could not be started: {exception.Message}");
        }

        _ = Process.Start(new ProcessStartInfo("wsl.exe")
        {
            Arguments = $"-d {instanceName} -u root --cd /root -- /bin/sh /root/manage-packs-bootstrap.sh",
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

    public void SaveOnboardingScript(string instanceName, string source, string? asUser = null)
    {
        string throughTheDrives = ToWslPath(source);
        string sourcePath = Quote(throughTheDrives);
        string user = Quote(asUser ?? string.Empty);
        string script = $$"""
            cp -f {{sourcePath}} /root/onboarding.sh || exit 1
            chmod 755 /root/onboarding.sh || exit 1
            printf '%s' {{user}} > /root/onboarding.user
            cat > /root/onboarding-run.sh <<'EOF'
            #!/bin/sh
            [ -x /root/onboarding.sh ] || exit 0
            if [ -s /root/onboarding.user ]; then
                /root/onboarding.sh "$(cat /root/onboarding.user)"
            else
                /root/onboarding.sh
            fi
            status=$?
            if [ "$status" -ne 0 ]; then
                printf '\nOnboarding failed (exit %s).\nPress Enter to close... ' "$status"
                read -r _
                exit "$status"
            fi
            rm -f /root/onboarding.sh /root/onboarding-run.sh /root/onboarding.user
            wsl_exe=""
            if command -v wsl.exe >/dev/null 2>&1; then
                wsl_exe=wsl.exe
            elif [ -x /mnt/c/Windows/System32/wsl.exe ]; then
                wsl_exe=/mnt/c/Windows/System32/wsl.exe
            fi
            if [ -n "$wsl_exe" ] && [ -n "${WSL_DISTRO_NAME:-}" ]; then
                "$wsl_exe" --terminate "$WSL_DISTRO_NAME" >/dev/null 2>&1 || true
            fi
            exit 0
            EOF
            chmod 755 /root/onboarding-run.sh || exit 1
            touch /root/.bashrc
            sed -i '/onboarding-run\.sh/d' /root/.bashrc || exit 1
            printf '%s\n' '[ -x /root/onboarding-run.sh ] && exec /root/onboarding-run.sh' >> /root/.bashrc
            touch /root/.zshenv
            sed -i '/onboarding-run\.sh/d' /root/.zshenv || exit 1
            printf '%s\n' '[[ -o interactive ]] && [ -x /root/onboarding-run.sh ] && exec /root/onboarding-run.sh' >> /root/.zshenv
            """;

        (int exitCode, _) = NativeCommand.RunWithInput(
            "wsl.exe",
            ["-d", instanceName, "-u", "root", "--", "sh", "-s"],
            script,
            includeError: true);

        if (exitCode != 0)
        {
            throw new WslSquadException(
                WslSquadErrorCode.WslOnboardingCopyFailed,
                source);
        }
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "'\\''") + "'";
    }

    private static string NormalizeShell(string script)
    {
        return script
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static string ToWslPath(string source)
    {
        string path = source.Replace('\\', '/');

        return "/mnt/" + char.ToLowerInvariant(path[0]) + path[2..];
    }


}
