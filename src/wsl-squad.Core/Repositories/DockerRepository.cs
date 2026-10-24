using WslSquad.Core.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;

using WslSquad.Core.Dtos;

namespace WslSquad.Core.Repositories;

public sealed record RegistryAnswer(RegistryValidationError Error, string? TechnicalDetail);

public sealed record SavedImageRow(string Name, string Path, DateTime Modified);

public sealed class DockerRepository : IDockerRepository
{
    private static readonly HttpClient _http = new();

    private readonly string _assetsDir;

    public DockerRepository(string assetsDir)
    {
        _assetsDir = assetsDir;
    }

    public RegistryAnswer TestRegistryImage(string reference)
    {
        if (!NativeCommand.Test("docker", ["--version"]))
        {
            return new RegistryAnswer(RegistryValidationError.DockerUnavailable, "docker is not on the PATH.");
        }

        bool onHub = true;
        string[] segments = reference.Split('/');

        if (segments.Length > 1)
        {
            string first = segments[0];

            if (first.Contains('.', StringComparison.Ordinal) ||
                first.Contains(':', StringComparison.Ordinal) ||
                first == "localhost")
            {
                onHub = false;
            }
        }

        if (reference.Contains('@', StringComparison.Ordinal))
        {
            onHub = false;
        }

        if (onHub)
        {
            string name = reference;
            string tag = "latest";
            int colon = reference.LastIndexOf(':');

            if (colon >= 0)
            {
                name = reference[..colon];
                tag = reference[(colon + 1)..];
            }

            if (!name.Contains('/', StringComparison.Ordinal))
            {
                name = "library/" + name;
            }

            string url = $"https://hub.docker.com/v2/repositories/{name}/tags/{tag}";

            try
            {
                using HttpRequestMessage request = new(HttpMethod.Head, url);
                using HttpResponseMessage answer = _http.Send(request);

                if (answer.StatusCode == HttpStatusCode.OK)
                {
                    return new RegistryAnswer(RegistryValidationError.None, null);
                }

                if (answer.StatusCode == HttpStatusCode.NotFound)
                {
                    return new RegistryAnswer(RegistryValidationError.NotFound, null);
                }

                return new RegistryAnswer(RegistryValidationError.Rejected, $"Docker Hub answered '{(int)answer.StatusCode}'.");
            }
            catch (HttpRequestException)
            {
                return new RegistryAnswer(RegistryValidationError.Unreachable, null);
            }
        }

        (int code, string output) = NativeCommand.Run("docker", ["manifest", "inspect", reference]);

        if (code == 0)
        {
            return new RegistryAnswer(RegistryValidationError.None, null);
        }

        string why = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        if (why.Length == 0)
        {
            why = $"docker exited with code {code}.";
        }

        return new RegistryAnswer(RegistryValidationError.Rejected, why);
    }

    public void Build(string tag, string dockerfile, string context, bool interactive = false)
    {
        string[] arguments = ["build", "-t", tag, "-f", dockerfile, context];
        int exitCode = interactive
            ? NativeCommand.RunAttached("docker", arguments)
            : NativeCommand.Run("docker", arguments).ExitCode;

        if (exitCode != 0)
        {
            throw new WslSquadException(WslSquadErrorCode.DockerOperationFailed, "build", $"exit code {exitCode}");
        }
    }

    public string Load(string tarPath, bool interactive = false)
    {
        (int code, string output) = interactive
            ? NativeCommand.RunInteractive("docker", ["load", "-i", tarPath])
            : NativeCommand.Run("docker", ["load", "-i", tarPath]);

        if (code != 0)
        {
            throw new WslSquadException(WslSquadErrorCode.DockerOperationFailed, "load", output);
        }

        foreach (string line in output.Split('\n'))
        {
            string text = line.Trim();

            if (text.StartsWith("Loaded image ID: ", StringComparison.Ordinal))
            {
                return text["Loaded image ID: ".Length..].Trim();
            }

            if (text.StartsWith("Loaded image: ", StringComparison.Ordinal))
            {
                return text["Loaded image: ".Length..].Trim();
            }
        }

        throw new WslSquadException(WslSquadErrorCode.DockerOperationFailed, tarPath, "docker load told nothing usable about the image");
    }

    public void ExportRootfs(string imageName, string outputPath, bool interactive = false)
    {
        string container = $"wsl-temp-export-{Guid.NewGuid().ToString()[..8]}";

        try
        {
            if (interactive)
            {
                int createCode = NativeCommand.RunAttached(
                    "docker",
                    ["create", "--name", container, imageName]);

                if (createCode != 0)
                {
                    throw new WslSquadException(
                        WslSquadErrorCode.DockerOperationFailed,
                        imageName,
                        $"could not create a container (exit code {createCode})");
                }

                int exportCode = NativeCommand.RunAttached(
                    "docker",
                    ["export", "-o", outputPath, container]);

                if (exportCode != 0)
                {
                    throw new WslSquadException(
                        WslSquadErrorCode.DockerOperationFailed,
                        imageName,
                        $"export failed (exit code {exportCode})");
                }
            }
            else
            {
                NativeCommand.Invoke(
                    "docker",
                    ["create", "--name", container, imageName],
                    WslSquadErrorCode.DockerOperationFailed,
                    imageName,
                    "create container");
                NativeCommand.Invoke(
                    "docker",
                    ["export", "-o", outputPath, container],
                    WslSquadErrorCode.DockerOperationFailed,
                    outputPath,
                    "export");
            }
        }
        finally
        {
            NativeCommand.Test("docker", ["rm", "-f", container]);
        }
    }

    public bool SaveImage(string tag, string outputPath)
    {
        string? folder = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return NativeCommand.Test("docker", ["save", "-o", outputPath, tag]);
    }

    public bool RemoveImage(string tag)
    {
        return NativeCommand.Test("docker", ["rmi", "-f", tag]);
    }

    public bool RestartDesktop()
    {
        return NativeCommand.Test("docker", ["desktop", "restart"]);
    }

    private static readonly TimeSpan _dockerStartupTimeout =
        TimeSpan.FromSeconds(60);

    private static readonly TimeSpan _dockerProbeTimeout =
        TimeSpan.FromSeconds(3);

    private static readonly TimeSpan _dockerProbeInterval =
        TimeSpan.FromSeconds(1);

    public bool StartDesktop()
    {
        if (IsDockerReady())
        {
            return true;
        }

        if (!TryStartDesktop())
        {
            Trace.TraceWarning(
                "[DockerRepository] Desktop launch request failed; " +
                "checking whether Docker becomes available anyway.");
        }

        return WaitForDockerReady(_dockerStartupTimeout);
    }

    private static bool TryStartDesktop()
    {
        string? programFiles =
            Environment.GetEnvironmentVariable("ProgramFiles");

        if (!string.IsNullOrEmpty(programFiles))
        {
            string executable = Path.Combine(
                programFiles,
                "Docker",
                "Docker",
                "Docker Desktop.exe");

            try
            {
                using Process? process = Process.Start(
                    new ProcessStartInfo(executable)
                    {
                        UseShellExecute = true
                    });

                if (process is not null)
                {
                    return true;
                }

                Trace.TraceWarning(
                    $"[DockerRepository] Process.Start returned null for '{executable}'.");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Trace.TraceWarning(
                    $"[DockerRepository] Direct launch failed: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                Trace.TraceWarning(
                    $"[DockerRepository] Docker Desktop executable not found: {ex.Message}");
            }
        }

        return NativeCommand.Test("docker", ["desktop", "start"]);
    }

    private static bool WaitForDockerReady(TimeSpan timeout)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        do
        {
            if (IsDockerReady())
            {
                return true;
            }

            if (stopwatch.Elapsed >= timeout)
            {
                break;
            }

            TimeSpan remaining = timeout - stopwatch.Elapsed;
            TimeSpan delay = remaining < _dockerProbeInterval
                ? remaining
                : _dockerProbeInterval;

            if (delay > TimeSpan.Zero)
            {
                Thread.Sleep(delay);
            }
        }
        while (stopwatch.Elapsed < timeout);

        Trace.TraceWarning(
            $"[DockerRepository] Docker did not become ready within {timeout}.");

        return false;
    }

    private static bool IsDockerReady()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info" },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process is null)
            {
                return false;
            }

            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(
                    (int)_dockerProbeTimeout.TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit();
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceWarning(
                        $"[DockerRepository] Process already stopped: {ex.Message}");
                }

                return false;
            }

            Task.WaitAll(stdout, stderr);

            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Trace.TraceWarning(
                $"[DockerRepository] Cannot execute docker info: {ex.Message}");

            return false;
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning(
                $"[DockerRepository] Docker readiness check failed: {ex.Message}");

            return false;
        }
    }

    public bool TestRunning()
    {
        return NativeCommand.Test("docker", ["info"]);
    }

    public string GetSavedImagePath(string name)
    {
        return Path.Combine(_assetsDir, "dockerimages", name, $"{name}.tar");
    }

    public IReadOnlyList<SavedImageRow> GetAllSavedImages()
    {
        string imageRoot = Path.Combine(_assetsDir, "dockerimages");

        if (!Directory.Exists(imageRoot))
        {
            return [];
        }

        List<SavedImageRow> images = [];

        foreach (string slot in Directory.EnumerateDirectories(imageRoot).Order(StringComparer.Ordinal))
        {
            string? file = Directory.EnumerateFiles(slot).FirstOrDefault();

            if (file is null)
            {
                continue;
            }

            images.Add(new SavedImageRow(
                Path.GetFileNameWithoutExtension(file),
                file,
                File.GetLastWriteTime(file)));
        }

        return images;
    }

    public IReadOnlyList<string>? GetDesktopNames()
    {
        JsonObject? config = ReadDesktopSettings(DesktopSettingsPath());

        return config?["IntegratedWslDistros"] is JsonArray names
            ? [.. names.Select(node => node?.GetValue<string>()).Where(name => !string.IsNullOrEmpty(name))!]
            : null;
    }

    public string GetDesktopState(string instanceName)
    {
        JsonObject? config = ReadDesktopSettings(DesktopSettingsPath());

        if (config?["IntegratedWslDistros"] is not JsonArray names)
        {
            return "unknown";
        }

        return names.Any(node => node?.GetValue<string>() == instanceName) ? "yes" : "no";
    }

    public void SetDesktopState(string instanceName)
    {
        WriteDesktopState(instanceName, add: true);
    }

    public void RemoveDesktopState(string instanceName)
    {
        WriteDesktopState(instanceName, add: false);
    }

    private void WriteDesktopState(string instanceName, bool add)
    {
        string path = DesktopSettingsPath();

        if (!File.Exists(path))
        {
            throw new WslSquadException(WslSquadErrorCode.DockerSettingsUnreadable, path);
        }

        JsonObject config = JsonNode.Parse(
            File.ReadAllText(path),
            documentOptions: new System.Text.Json.JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            })?.AsObject() ?? [];

        List<string> names = config["IntegratedWslDistros"] is JsonArray existing
            ? [.. existing.Select(node => node?.GetValue<string>()).OfType<string>().Where(name => name.Length > 0)]
            : [];

        int removed = names.RemoveAll(name => name == instanceName);

        if (add)
        {
            names.Add(instanceName);
        }
        else if (removed == 0)
        {
            return;
        }

        config["IntegratedWslDistros"] = new JsonArray([.. names.Select(name => JsonValue.Create(name))]);

        File.Copy(path, path + ".bak", overwrite: true);
        AtomicFile.WriteText(path, config.ToJsonString(AtomicFile.JsonOptions));
    }

    private static JsonObject? ReadDesktopSettings(string path)
    {
        if (path.Length == 0 || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(
                File.ReadAllText(path),
                documentOptions: new System.Text.Json.JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                })?.AsObject();
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string DesktopSettingsPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return appData.Length == 0 ? string.Empty : Path.Combine(appData, "Docker", "settings-store.json");
    }
}
