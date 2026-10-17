using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Reads and writes the <c>.wsl-squad</c> ownership marker.</summary>
public sealed class FleetMarkerStore
{
    private readonly IFileSystem _fileSystem;

    public FleetMarkerStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string GetMarkerPath(string instanceDirectory)
    {
        return _fileSystem.Path.Combine(instanceDirectory, FleetMarker.FileName);
    }

    public bool Exists(string instanceDirectory)
    {
        return _fileSystem.File.Exists(GetMarkerPath(instanceDirectory));
    }

    public OperationResult<FleetMarker> Read(string instanceDirectory)
    {
        string markerPath = GetMarkerPath(instanceDirectory);

        if (!_fileSystem.File.Exists(markerPath))
        {
            return OperationResult<FleetMarker>.Fail(
                ErrorCode.NotFound,
                $"Instance marker '{markerPath}' does not exist.");
        }

        try
        {
            FleetMarker? marker = JsonSerializer.Deserialize<FleetMarker>(
                _fileSystem.File.ReadAllText(markerPath),
                JsonDefaults.Options);

            return marker is null
                ? OperationResult<FleetMarker>.Fail(ErrorCode.CorruptState, $"Instance marker '{markerPath}' is empty.")
                : OperationResult<FleetMarker>.Ok(marker);
        }
        catch (JsonException exception)
        {
            return OperationResult<FleetMarker>.Fail(
                ErrorCode.CorruptState,
                $"Instance marker '{markerPath}' is not valid JSON.",
                exception.Message);
        }
    }

    public OperationResult Write(string instanceDirectory, FleetMarker marker)
    {
        string markerPath = GetMarkerPath(instanceDirectory);

        try
        {
            AtomicFileWriter.WriteAllText(
                _fileSystem,
                markerPath,
                JsonSerializer.Serialize(marker, JsonDefaults.Options));

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Instance marker '{markerPath}' could not be written.",
                exception.Message);
        }
    }
}
