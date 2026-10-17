using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Json;

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
