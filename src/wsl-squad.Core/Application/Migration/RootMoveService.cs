using System.IO.Abstractions;

using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Migration;

/// <summary>Moves a fleet to another folder by archiving, removing and relocating archives.</summary>
public sealed class RootMoveService
{
    private static readonly string[] _reservedFolders =
        [ArchiveStore.ArchivesFolderName, ".staging", ".locks"];

    private readonly IFileSystem _fileSystem;
    private readonly FleetMarkerStore _markers;
    private readonly ArchiveService _archiveService;
    private readonly RemoveInstanceHandler _removeHandler;
    private readonly ArchiveStore _archives;
    private readonly FleetConfigStore _config;

    public RootMoveService(
        IFileSystem fileSystem,
        FleetMarkerStore markers,
        ArchiveService archiveService,
        RemoveInstanceHandler removeHandler,
        ArchiveStore archives,
        FleetConfigStore config)
    {
        _fileSystem = fileSystem;
        _markers = markers;
        _archiveService = archiveService;
        _removeHandler = removeHandler;
        _archives = archives;
        _config = config;
    }

    public async Task<OperationResult<RootMoveOutcome>> MoveAsync(
        string sourceRoot,
        string targetRoot,
        CancellationToken cancellationToken = default)
    {
        OperationResult validation = Validate(sourceRoot, targetRoot);

        if (!validation.IsSuccess)
        {
            return OperationFailure.From<RootMoveOutcome>(validation.Error!);
        }

        List<string> names = FindManagedInstances(sourceRoot);
        List<string> warnings = [];

        foreach (string name in names)
        {
            OperationResult<ArchiveOutcome> archived = await _archiveService
                .ArchiveAsync(
                    sourceRoot,
                    new ArchiveRequest(name, null, ArchiveFormat.Default, Replace: true, StopIfRunning: true),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!archived.IsSuccess)
            {
                return OperationFailure.From<RootMoveOutcome>(archived.Error!);
            }

            warnings.AddRange(archived.Warnings);
        }

        List<string> removed = [];

        foreach (string name in names)
        {
            OperationResult<RemoveOutcome> result = await _removeHandler
                .RemoveAsync(sourceRoot, new RemoveRequest(name, ArchiveFirst: false), cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return OperationResult<RootMoveOutcome>.Fail(
                    result.Error!.Code,
                    "The fleet could not be moved; some instances were already removed.",
                    OperationFailure.Combine(
                        result.Error.Detail,
                        [$"Removed before the failure: {string.Join(", ", removed)}"]));
            }

            removed.Add(name);
            warnings.AddRange(result.Warnings);
        }

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(targetRoot);
            string sourceArchives = _archives.GetArchivesRoot(sourceRoot);
            string targetArchives = _archives.GetArchivesRoot(targetRoot);

            if (_fileSystem.Directory.Exists(sourceArchives))
            {
                MoveDirectory(sourceArchives, targetArchives);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult<RootMoveOutcome>.Fail(
                ErrorCode.IoError,
                "Instances were removed, but the archives could not be moved.",
                exception.Message);
        }

        OperationResult saved = _config.Save(new FleetConfig { FleetRoot = targetRoot });

        if (!saved.IsSuccess)
        {
            return OperationFailure.From<RootMoveOutcome>(saved.Error!);
        }

        return warnings.Count == 0
            ? OperationResult<RootMoveOutcome>.Ok(new RootMoveOutcome(sourceRoot, targetRoot, removed, warnings))
            : OperationResult<RootMoveOutcome>.Warn(
                new RootMoveOutcome(sourceRoot, targetRoot, removed, warnings),
                [.. warnings]);
    }

    private OperationResult Validate(string sourceRoot, string targetRoot)
    {
        if (!PathHelper.IsFullyQualified(sourceRoot) || !_fileSystem.Directory.Exists(sourceRoot))
        {
            return OperationResult.Fail(ErrorCode.NotFound, $"Fleet root '{sourceRoot}' does not exist.");
        }

        if (!PathHelper.IsFullyQualified(targetRoot))
        {
            return OperationResult.Fail(ErrorCode.Validation, $"Target root '{targetRoot}' is not absolute.");
        }

        if (PathHelper.AreSamePath(sourceRoot, targetRoot) ||
            PathHelper.IsUnder(targetRoot, sourceRoot) ||
            PathHelper.IsUnder(sourceRoot, targetRoot))
        {
            return OperationResult.Fail(
                ErrorCode.Validation,
                "The target root must be outside the current fleet root.");
        }

        if (_fileSystem.Directory.Exists(targetRoot) &&
            _fileSystem.Directory.EnumerateFileSystemEntries(targetRoot).Any())
        {
            return OperationResult.Fail(
                ErrorCode.AlreadyExists,
                $"Target root '{targetRoot}' is not empty.");
        }

        if (_fileSystem.Directory.Exists(_archives.GetArchivesRoot(targetRoot)))
        {
            return OperationResult.Fail(
                ErrorCode.AlreadyExists,
                $"Target root '{targetRoot}' already contains an archives folder.");
        }

        return OperationResult.Ok();
    }

    private List<string> FindManagedInstances(string sourceRoot)
    {
        List<string> names = [];

        try
        {
            foreach (string directory in _fileSystem.Directory.GetDirectories(sourceRoot))
            {
                string name = _fileSystem.Path.GetFileName(directory);

                if (_reservedFolders.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    !InstanceName.IsValid(name) ||
                    !_markers.Exists(directory))
                {
                    continue;
                }

                names.Add(name);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return names;
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);

        return names;
    }

    private void MoveDirectory(string source, string target)
    {
        try
        {
            _fileSystem.Directory.Move(source, target);
        }
        catch (IOException)
        {
            CopyDirectory(source, target);
            _fileSystem.Directory.Delete(source, recursive: true);
        }
    }

    private void CopyDirectory(string source, string target)
    {
        _ = _fileSystem.Directory.CreateDirectory(target);

        foreach (string file in _fileSystem.Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = _fileSystem.Path.GetRelativePath(source, file);
            string destination = _fileSystem.Path.Combine(target, relative);
            string? directory = _fileSystem.Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(directory))
            {
                _ = _fileSystem.Directory.CreateDirectory(directory);
            }

            _fileSystem.File.Copy(file, destination, overwrite: true);
        }
    }
}
