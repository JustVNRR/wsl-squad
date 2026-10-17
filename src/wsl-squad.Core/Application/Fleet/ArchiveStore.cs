using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Lists archive folders under the fleet root.</summary>
public sealed class ArchiveStore
{
    public const string ArchivesFolderName = "archives";

    private readonly IFileSystem _fileSystem;

    public ArchiveStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string GetArchivesRoot(string fleetRoot)
    {
        return _fileSystem.Path.Combine(fleetRoot, ArchivesFolderName);
    }

    public string GetArchiveDirectory(string fleetRoot, string archiveName)
    {
        return _fileSystem.Path.Combine(GetArchivesRoot(fleetRoot), archiveName);
    }

    public OperationResult<ArchiveMetadata> Read(string archiveDirectory)
    {
        string metadataPath = _fileSystem.Path.Combine(archiveDirectory, ArchiveMetadata.FileName);

        if (!_fileSystem.File.Exists(metadataPath))
        {
            return OperationResult<ArchiveMetadata>.Fail(
                ErrorCode.NotFound,
                $"Archive metadata '{metadataPath}' does not exist.");
        }

        try
        {
            ArchiveMetadata? metadata = JsonSerializer.Deserialize<ArchiveMetadata>(
                _fileSystem.File.ReadAllText(metadataPath),
                JsonDefaults.Options);

            return metadata is null
                ? OperationResult<ArchiveMetadata>.Fail(
                    ErrorCode.CorruptState,
                    $"Archive metadata '{metadataPath}' is empty.")
                : OperationResult<ArchiveMetadata>.Ok(metadata);
        }
        catch (JsonException exception)
        {
            return OperationResult<ArchiveMetadata>.Fail(
                ErrorCode.CorruptState,
                $"Archive metadata '{metadataPath}' is not valid JSON.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult<ArchiveMetadata>.Fail(
                ErrorCode.IoError,
                $"Archive metadata '{metadataPath}' could not be read.",
                exception.Message);
        }
    }

    public OperationResult Write(string archiveDirectory, ArchiveMetadata metadata)
    {
        string metadataPath = _fileSystem.Path.Combine(archiveDirectory, ArchiveMetadata.FileName);

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
                $"Archive metadata '{metadataPath}' could not be written.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                $"Archive metadata '{metadataPath}' could not be written.",
                exception.Message);
        }
    }

    public OperationResult<ArchiveListing> List(string fleetRoot)
    {
        string archivesRoot = GetArchivesRoot(fleetRoot);
        List<WslArchiveInfo> archives = [];
        List<string> warnings = [];

        if (!_fileSystem.Directory.Exists(archivesRoot))
        {
            return OperationResult<ArchiveListing>.Ok(new ArchiveListing(archives, warnings));
        }

        try
        {
            foreach (string archiveDirectory in _fileSystem.Directory.GetDirectories(archivesRoot))
            {
                string archiveName = _fileSystem.Path.GetFileName(archiveDirectory);
                string metadataPath = _fileSystem.Path.Combine(archiveDirectory, ArchiveMetadata.FileName);

                if (!_fileSystem.File.Exists(metadataPath))
                {
                    warnings.Add($"Archive '{archiveName}' has no {ArchiveMetadata.FileName}.");
                    continue;
                }

                ArchiveMetadata? metadata;
                try
                {
                    metadata = JsonSerializer.Deserialize<ArchiveMetadata>(
                        _fileSystem.File.ReadAllText(metadataPath),
                        JsonDefaults.Options);
                }
                catch (JsonException exception)
                {
                    warnings.Add($"Archive '{archiveName}' has an invalid {ArchiveMetadata.FileName}: {exception.Message}");
                    continue;
                }

                if (metadata is null || string.IsNullOrWhiteSpace(metadata.File))
                {
                    warnings.Add($"Archive '{archiveName}' does not name its archive file.");
                    continue;
                }

                string archiveFile = _fileSystem.Path.Combine(archiveDirectory, metadata.File);
                if (!_fileSystem.File.Exists(archiveFile))
                {
                    warnings.Add($"Archive '{archiveName}' is missing its file '{metadata.File}'.");
                    continue;
                }

                long sizeBytes = _fileSystem.FileInfo.New(archiveFile).Length;
                archives.Add(new WslArchiveInfo(
                    archiveName,
                    archiveDirectory,
                    metadata.File,
                    metadata.Format,
                    metadata.CreatedUtc,
                    sizeBytes,
                    metadata));
            }
        }
        catch (IOException exception)
        {
            return OperationResult<ArchiveListing>.Fail(
                ErrorCode.IoError,
                $"Archives under '{archivesRoot}' could not be listed.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<ArchiveListing>.Fail(
                ErrorCode.PermissionDenied,
                $"Archives under '{archivesRoot}' could not be listed.",
                exception.Message);
        }

        archives.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        return OperationResult<ArchiveListing>.Ok(new ArchiveListing(archives, warnings));
    }
}
