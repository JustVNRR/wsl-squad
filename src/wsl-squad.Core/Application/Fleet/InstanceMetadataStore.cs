using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Reads and writes <c>instance.json</c>.</summary>
public sealed class InstanceMetadataStore
{
    private readonly IFileSystem _fileSystem;
    private readonly IClock _clock;

    public InstanceMetadataStore(IFileSystem fileSystem, IClock clock)
    {
        _fileSystem = fileSystem;
        _clock = clock;
    }

    public string GetMetadataPath(string instanceDirectory)
    {
        return _fileSystem.Path.Combine(instanceDirectory, InstanceMetadata.FileName);
    }

    public OperationResult<InstanceMetadata> Read(string instanceDirectory)
    {
        string metadataPath = GetMetadataPath(instanceDirectory);

        if (!_fileSystem.File.Exists(metadataPath))
        {
            return OperationResult<InstanceMetadata>.Fail(
                ErrorCode.NotFound,
                $"Instance metadata '{metadataPath}' does not exist.");
        }

        try
        {
            InstanceMetadata? metadata = JsonSerializer.Deserialize<InstanceMetadata>(
                _fileSystem.File.ReadAllText(metadataPath),
                JsonDefaults.Options);

            return metadata is null
                ? OperationResult<InstanceMetadata>.Fail(
                    ErrorCode.CorruptState,
                    $"Instance metadata '{metadataPath}' is empty.")
                : OperationResult<InstanceMetadata>.Ok(metadata);
        }
        catch (JsonException exception)
        {
            return OperationResult<InstanceMetadata>.Fail(
                ErrorCode.CorruptState,
                $"Instance metadata '{metadataPath}' is not valid JSON.",
                exception.Message);
        }
    }

    public OperationResult Save(string instanceDirectory, InstanceMetadata metadata)
    {
        string metadataPath = GetMetadataPath(instanceDirectory);
        metadata.UpdatedUtc = _clock.UtcNow;

        try
        {
            AtomicFileWriter.WriteAllText(
                _fileSystem,
                metadataPath,
                JsonSerializer.Serialize(metadata, JsonDefaults.Options));

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Instance metadata '{metadataPath}' could not be written.",
                exception.Message);
        }
    }
}
