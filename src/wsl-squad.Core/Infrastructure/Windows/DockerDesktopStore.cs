using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

using wsl_squad.Core.Abstractions.Platform;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Edits <c>%APPDATA%\Docker\settings-store.json</c> while preserving unknown fields.</summary>
public sealed class DockerDesktopStore : IDockerDesktopStore
{
    private const string _integratedKey = "IntegratedWslDistros";

    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IFileSystem _fileSystem;

    public DockerDesktopStore(IFileSystem fileSystem, IPlatformEnvironment platform)
    {
        _fileSystem = fileSystem;
        SettingsPath = fileSystem.Path.Combine(platform.RoamingApplicationData, "Docker", "settings-store.json");
    }

    public string SettingsPath { get; }

    public OperationResult<DockerDesktopSettings> Read()
    {
        if (!_fileSystem.File.Exists(SettingsPath))
        {
            return OperationResult<DockerDesktopSettings>.Ok(new DockerDesktopSettings(false, []));
        }

        try
        {
            string json = _fileSystem.File.ReadAllText(SettingsPath);
            JsonNode? root = JsonNode.Parse(json, documentOptions: _documentOptions);

            if (root is not JsonObject settings)
            {
                return OperationResult<DockerDesktopSettings>.Fail(
                    ErrorCode.CorruptState,
                    $"Docker Desktop settings '{SettingsPath}' are not a JSON object.");
            }

            List<string> distros = ReadIntegratedDistros(settings);

            return OperationResult<DockerDesktopSettings>.Ok(new DockerDesktopSettings(true, distros));
        }
        catch (JsonException exception)
        {
            return OperationResult<DockerDesktopSettings>.Fail(
                ErrorCode.CorruptState,
                $"Docker Desktop settings '{SettingsPath}' are not valid JSON.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult<DockerDesktopSettings>.Fail(
                ErrorCode.IoError,
                $"Docker Desktop settings '{SettingsPath}' could not be read.",
                exception.Message);
        }
    }

    public OperationResult SetIntegration(string instanceName, bool integrated)
    {
        if (!_fileSystem.File.Exists(SettingsPath))
        {
            return OperationResult.Fail(
                ErrorCode.NotFound,
                $"Docker Desktop settings '{SettingsPath}' were not found. Start Docker Desktop first.");
        }

        try
        {
            string json = _fileSystem.File.ReadAllText(SettingsPath);
            JsonNode? root = JsonNode.Parse(json, documentOptions: _documentOptions);

            if (root is not JsonObject settings)
            {
                return OperationResult.Fail(
                    ErrorCode.CorruptState,
                    $"Docker Desktop settings '{SettingsPath}' are not a JSON object.");
            }

            List<string> distros = ReadIntegratedDistros(settings);
            bool changed = integrated
                ? AddDistro(distros, instanceName)
                : RemoveDistro(distros, instanceName);

            if (!changed)
            {
                return OperationResult.Ok();
            }

            settings[_integratedKey] = new JsonArray([.. distros.Select(name => JsonValue.Create(name))]);

            _fileSystem.File.Copy(SettingsPath, SettingsPath + ".bak", overwrite: true);
            AtomicFileWriter.WriteAllText(
                _fileSystem,
                SettingsPath,
                settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            return OperationResult.Ok();
        }
        catch (JsonException exception)
        {
            return OperationResult.Fail(
                ErrorCode.CorruptState,
                $"Docker Desktop settings '{SettingsPath}' are not valid JSON.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Docker Desktop settings '{SettingsPath}' could not be written.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                $"Docker Desktop settings '{SettingsPath}' could not be written.",
                exception.Message);
        }
    }

    private static List<string> ReadIntegratedDistros(JsonObject settings)
    {
        List<string> distros = [];

        if (settings[_integratedKey] is JsonArray array)
        {
            foreach (JsonNode? node in array)
            {
                string? name = node?.GetValue<string>();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    distros.Add(name);
                }
            }
        }

        return distros;
    }

    private static bool AddDistro(List<string> distros, string instanceName)
    {
        if (distros.Contains(instanceName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        distros.Add(instanceName);

        return true;
    }

    private static bool RemoveDistro(List<string> distros, string instanceName)
    {
        int index = distros.FindIndex(name => string.Equals(name, instanceName, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return false;
        }

        distros.RemoveAt(index);

        return true;
    }
}
