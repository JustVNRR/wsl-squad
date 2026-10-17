using System.IO.Abstractions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Deletes an archive folder under the fleet root.</summary>
public sealed class DeleteArchiveHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly ArchiveStore _archives;

    public DeleteArchiveHandler(IFileSystem fileSystem, ArchiveStore archives)
    {
        _fileSystem = fileSystem;
        _archives = archives;
    }

    public OperationResult<DeleteArchiveOutcome> Delete(string fleetRoot, string archiveName)
    {
        if (!InstanceName.IsValid(archiveName))
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.InvalidName,
                $"Archive name '{archiveName}' is invalid.");
        }

        string archivesRoot = _archives.GetArchivesRoot(fleetRoot);
        string archiveDirectory = _archives.GetArchiveDirectory(fleetRoot, archiveName);

        if (!PathHelper.IsUnder(archiveDirectory, archivesRoot))
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.Validation,
                $"Archive path '{archiveDirectory}' is outside the archives root.");
        }

        if (!_fileSystem.Directory.Exists(archiveDirectory))
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.NotFound,
                $"Archive '{archiveName}' does not exist.");
        }

        string metadataPath = _fileSystem.Path.Combine(archiveDirectory, ArchiveMetadata.FileName);

        if (!_fileSystem.File.Exists(metadataPath))
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.NotFound,
                $"Archive '{archiveName}' is not a managed archive.");
        }

        try
        {
            _fileSystem.Directory.Delete(archiveDirectory, recursive: true);

            return OperationResult<DeleteArchiveOutcome>.Ok(new DeleteArchiveOutcome(archiveName));
        }
        catch (IOException exception)
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.IoError,
                $"Archive '{archiveName}' could not be deleted.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<DeleteArchiveOutcome>.Fail(
                ErrorCode.PermissionDenied,
                $"Archive '{archiveName}' could not be deleted.",
                exception.Message);
        }
    }
}
