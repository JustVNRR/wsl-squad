using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Reads and writes <c>config.json</c>.</summary>
public sealed class FleetConfigStore
{
    private readonly IFileSystem _fileSystem;

    public FleetConfigStore(IFileSystem fileSystem, string configPath)
    {
        _fileSystem = fileSystem;
        ConfigPath = configPath;
    }

    public string ConfigPath { get; }

    public OperationResult<FleetConfig?> Load()
    {
        if (!_fileSystem.File.Exists(ConfigPath))
        {
            return OperationResult<FleetConfig?>.Ok(null);
        }

        try
        {
            string json = _fileSystem.File.ReadAllText(ConfigPath);
            FleetConfig? config = JsonSerializer.Deserialize<FleetConfig>(json, JsonDefaults.Options);

            if (config is null || string.IsNullOrWhiteSpace(config.FleetRoot))
            {
                return OperationResult<FleetConfig?>.Fail(
                    ErrorCode.CorruptState,
                    $"Configuration file '{ConfigPath}' does not contain a fleet root.");
            }

            return OperationResult<FleetConfig?>.Ok(config);
        }
        catch (JsonException exception)
        {
            return OperationResult<FleetConfig?>.Fail(
                ErrorCode.CorruptState,
                $"Configuration file '{ConfigPath}' is not valid JSON.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult<FleetConfig?>.Fail(
                ErrorCode.IoError,
                $"Configuration file '{ConfigPath}' could not be read.",
                exception.Message);
        }
    }

    public OperationResult Save(FleetConfig config)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, JsonDefaults.Options);
            AtomicFileWriter.WriteAllText(_fileSystem, ConfigPath, json);

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Configuration file '{ConfigPath}' could not be written.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                $"Configuration file '{ConfigPath}' could not be written.",
                exception.Message);
        }
    }
}
