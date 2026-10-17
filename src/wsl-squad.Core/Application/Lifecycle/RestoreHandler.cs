using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Storage;
using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Restores an instance from an archive folder.</summary>
public sealed class RestoreHandler
{
    private const long _freeSpaceMargin = 64L * 1024 * 1024;

    private readonly IFileSystem _fileSystem;
    private readonly IWslGateway _wsl;
    private readonly IWslRegistryGateway _registry;
    private readonly ArchiveStore _archives;
    private readonly FleetMarkerStore _markers;
    private readonly InstanceMetadataStore _metadata;
    private readonly IDiskSpaceProbe _diskSpace;
    private readonly IClock _clock;

    public RestoreHandler(
        IFileSystem fileSystem,
        IWslGateway wsl,
        IWslRegistryGateway registry,
        ArchiveStore archives,
        FleetMarkerStore markers,
        InstanceMetadataStore metadata,
        IDiskSpaceProbe diskSpace,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _wsl = wsl;
        _registry = registry;
        _archives = archives;
        _markers = markers;
        _metadata = metadata;
        _diskSpace = diskSpace;
        _clock = clock;
    }

    public async Task<OperationResult<RestoreOutcome>> RestoreAsync(
        string fleetRoot,
        RestoreRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!InstanceName.IsValid(request.InstanceName))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.InvalidName,
                $"Instance name '{request.InstanceName}' is invalid.");
        }

        if (!InstanceName.IsValid(request.ArchiveName))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.InvalidName,
                $"Archive name '{request.ArchiveName}' is invalid.");
        }

        string archivesRoot = _archives.GetArchivesRoot(fleetRoot);
        string archiveDirectory = _archives.GetArchiveDirectory(fleetRoot, request.ArchiveName);

        if (!PathHelper.IsUnder(archiveDirectory, archivesRoot))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.Validation,
                $"Archive path '{archiveDirectory}' is outside the archives root.");
        }

        OperationResult<ArchiveMetadata> metadata = _archives.Read(archiveDirectory);

        if (!metadata.IsSuccess)
        {
            return OperationFailure.From<RestoreOutcome>(metadata.Error!);
        }

        string archiveFile = _fileSystem.Path.Combine(archiveDirectory, metadata.Value.File);

        if (!PathHelper.IsUnder(archiveFile, archiveDirectory) || !_fileSystem.File.Exists(archiveFile))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.NotFound,
                $"Archive file '{metadata.Value.File}' does not exist.");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Value.Sha256))
        {
            string actualHash = FileHash.Sha256(_fileSystem, archiveFile);

            if (!string.Equals(actualHash, metadata.Value.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<RestoreOutcome>.Fail(
                    ErrorCode.CorruptState,
                    $"Archive '{request.ArchiveName}' failed its checksum.");
            }
        }

        if (IsRegisteredAlready(request.InstanceName))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.AlreadyExists,
                $"A WSL distribution named '{request.InstanceName}' already exists.");
        }

        string installPath = _fileSystem.Path.Combine(fleetRoot, request.InstanceName);

        if (_fileSystem.Directory.Exists(installPath))
        {
            return OperationResult<RestoreOutcome>.Fail(
                ErrorCode.AlreadyExists,
                $"Fleet folder '{installPath}' already exists.");
        }

        long archiveSize = GetFileSize(archiveFile);
        OperationResult space = CheckFreeSpace(fleetRoot, archiveSize);

        if (!space.IsSuccess)
        {
            return OperationFailure.From<RestoreOutcome>(space.Error!);
        }

        bool imported = false;

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(installPath);

            try
            {
                await _wsl.ImportDistributionAsync(
                    request.InstanceName,
                    installPath,
                    archiveFile,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (OperationFailure.IsExpected(exception))
            {
                await TryUnregisterAsync(request.InstanceName).ConfigureAwait(false);
                TryDeleteDirectory(installPath);

                return OperationFailure.From<RestoreOutcome>(exception);
            }

            imported = true;

            string id = Guid.NewGuid().ToString("N");
            DateTimeOffset now = _clock.UtcNow;

            OperationResult marker = _markers.Write(installPath, new FleetMarker
            {
                Id = id,
                Name = request.InstanceName,
                CreatedUtc = now,
                CreatedBy = "restore",
            });

            if (!marker.IsSuccess)
            {
                await RollbackAsync(request.InstanceName, installPath).ConfigureAwait(false);

                return OperationFailure.From<RestoreOutcome>(marker.Error!);
            }

            InstanceMetadata instance = new()
            {
                Id = id,
                Name = request.InstanceName,
                CreatedUtc = now,
                DefaultUser = metadata.Value.DefaultUser,
                DockerDesktop = metadata.Value.DockerDesktop,
                Appearance = metadata.Value.Appearance,
                Source = new InstanceSource
                {
                    Kind = "Restore",
                    Reference = request.ArchiveName,
                },
            };

            OperationResult saved = _metadata.Save(installPath, instance);

            if (!saved.IsSuccess)
            {
                await RollbackAsync(request.InstanceName, installPath).ConfigureAwait(false);

                return OperationFailure.From<RestoreOutcome>(saved.Error!);
            }

            CopyIcon(archiveDirectory, installPath);

            return OperationResult<RestoreOutcome>.Ok(
                new RestoreOutcome(request.InstanceName, request.ArchiveName, installPath, instance.DefaultUser));
        }
        catch (OperationCanceledException)
        {
            if (imported)
            {
                await RollbackAsync(request.InstanceName, installPath).ConfigureAwait(false);
            }

            return OperationResult<RestoreOutcome>.Fail(ErrorCode.Cancelled, "Restore cancelled.");
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            if (imported)
            {
                await RollbackAsync(request.InstanceName, installPath).ConfigureAwait(false);
            }
            else
            {
                TryDeleteDirectory(installPath);
            }

            return OperationFailure.From<RestoreOutcome>(exception);
        }
    }

    private bool IsRegisteredAlready(string name)
    {
        if (!_registry.IsSupported)
        {
            return false;
        }

        try
        {
            return _registry.GetDistributions()
                .Any(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private OperationResult CheckFreeSpace(string path, long requiredBytes)
    {
        try
        {
            _ = _fileSystem.Directory.CreateDirectory(path);
            long available = _diskSpace.GetAvailableFreeBytes(path);

            return available > 0 && available < requiredBytes + _freeSpaceMargin
                ? OperationResult.Fail(
                    ErrorCode.DiskSpace,
                    $"Not enough space to restore: {requiredBytes} bytes needed.")
                : OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(ErrorCode.IoError, "Free space could not be checked.", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(ErrorCode.PermissionDenied, "Free space could not be checked.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return OperationResult.Fail(ErrorCode.Validation, "Free space could not be checked.", exception.Message);
        }
    }

    private long GetFileSize(string path)
    {
        try
        {
            return _fileSystem.FileInfo.New(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private async Task RollbackAsync(string name, string installPath)
    {
        await TryUnregisterAsync(name).ConfigureAwait(false);
        TryDeleteDirectory(installPath);
    }

    private async Task TryUnregisterAsync(string name)
    {
        try
        {
            await _wsl.UnregisterDistributionAsync(name, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            // The distribution may not have been registered.
        }
    }

    private void CopyIcon(string archiveDirectory, string installPath)
    {
        string source = _fileSystem.Path.Combine(archiveDirectory, "terminal-icon.png");

        if (!_fileSystem.File.Exists(source))
        {
            return;
        }

        try
        {
            _fileSystem.File.Copy(source, _fileSystem.Path.Combine(installPath, "terminal-icon.png"), true);
        }
        catch (IOException)
        {
            // Icons are cosmetic; restore succeeds without them.
        }
        catch (UnauthorizedAccessException)
        {
            // Icons are cosmetic; restore succeeds without them.
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (_fileSystem.Directory.Exists(path))
            {
                _fileSystem.Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
