using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Storage;
using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Copies an instance into a new WSL distribution.</summary>
public sealed class DuplicateHandler
{
    private const long _freeSpaceMargin = 64L * 1024 * 1024;

    private readonly IFileSystem _fileSystem;
    private readonly InstanceStateService _instances;
    private readonly IWslRegistryGateway _registry;
    private readonly IWslGateway _wsl;
    private readonly FleetMarkerStore _markers;
    private readonly InstanceMetadataStore _metadata;
    private readonly ArchiveStore _archives;
    private readonly IDiskSpaceProbe _diskSpace;
    private readonly IClock _clock;

    public DuplicateHandler(
        IFileSystem fileSystem,
        InstanceStateService instances,
        IWslRegistryGateway registry,
        IWslGateway wsl,
        FleetMarkerStore markers,
        InstanceMetadataStore metadata,
        ArchiveStore archives,
        IDiskSpaceProbe diskSpace,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _instances = instances;
        _registry = registry;
        _wsl = wsl;
        _markers = markers;
        _metadata = metadata;
        _archives = archives;
        _diskSpace = diskSpace;
        _clock = clock;
    }

    public async Task<OperationResult<DuplicateOutcome>> DuplicateAsync(
        string fleetRoot,
        DuplicateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!InstanceName.IsValid(request.TargetName))
        {
            return OperationResult<DuplicateOutcome>.Fail(
                ErrorCode.InvalidName,
                $"Instance name '{request.TargetName}' is invalid.");
        }

        if (string.Equals(request.SourceName, request.TargetName, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<DuplicateOutcome>.Fail(
                ErrorCode.Validation,
                "Source and target names must differ.");
        }

        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, request.SourceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<DuplicateOutcome>(managed.Error!);
        }

        string sourcePath = managed.Value;
        string targetPath = _fileSystem.Path.Combine(fleetRoot, request.TargetName);

        if (IsRegisteredAlready(request.TargetName) ||
            _fileSystem.Directory.Exists(targetPath) ||
            _fileSystem.Directory.Exists(_archives.GetArchiveDirectory(fleetRoot, $"{request.TargetName}_archive")))
        {
            return OperationResult<DuplicateOutcome>.Fail(
                ErrorCode.AlreadyExists,
                $"Instance or archive '{request.TargetName}' already exists.");
        }

        long sourceSize = _instances.GetVirtualDiskSize(sourcePath);
        OperationResult space = CheckFreeSpace(fleetRoot, sourceSize * 2);

        if (!space.IsSuccess)
        {
            return OperationFailure.From<DuplicateOutcome>(space.Error!);
        }

        OperationResult<bool> running = await _instances
            .IsRunningAsync(request.SourceName, cancellationToken)
            .ConfigureAwait(false);

        if (!running.IsSuccess)
        {
            return OperationFailure.From<DuplicateOutcome>(running.Error!);
        }

        bool wasRunning = running.Value;
        bool stopRequested = false;
        string operationId = Guid.NewGuid().ToString("N");
        string stagingRoot = _fileSystem.Path.Combine(fleetRoot, ".staging", operationId);
        string exportTar = _fileSystem.Path.Combine(stagingRoot, $"{request.TargetName}-export.tar.gz");
        OperationTransaction transaction = new();
        List<string> warnings = [];

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(stagingRoot);

            if (wasRunning)
            {
                await _wsl.TerminateDistributionAsync(request.SourceName, cancellationToken)
                    .ConfigureAwait(false);
                stopRequested = true;
            }

            await _wsl.ExportDistributionAsync(request.SourceName, exportTar, ArchiveFormat.Default, cancellationToken)
                .ConfigureAwait(false);

            _ = _fileSystem.Directory.CreateDirectory(targetPath);
            transaction.OnRollback(() =>
            {
                TryDeleteDirectory(targetPath);
                return Task.CompletedTask;
            });

            try
            {
                await _wsl.ImportDistributionAsync(request.TargetName, targetPath, exportTar, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (OperationFailure.IsExpected(exception))
            {
                await TryUnregisterAsync(request.TargetName).ConfigureAwait(false);
                IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);
                _ = await TryRestartAsync(wasRunning, stopRequested, request.SourceName, warnings)
                    .ConfigureAwait(false);

                return OperationFailure.From<DuplicateOutcome>(
                    exception,
                    [.. warnings, .. rollbackFailures]);
            }

            transaction.OnRollback(() => _wsl.UnregisterDistributionAsync(request.TargetName, CancellationToken.None));

            string id = Guid.NewGuid().ToString("N");
            DateTimeOffset now = _clock.UtcNow;
            OperationResult<InstanceMetadata> sourceMetadata = _metadata.Read(sourcePath);

            OperationResult marker = _markers.Write(targetPath, new FleetMarker
            {
                Id = id,
                Name = request.TargetName,
                CreatedUtc = now,
                CreatedBy = "duplicate",
            });

            if (!marker.IsSuccess)
            {
                return await FailWithRollbackAsync(transaction, marker.Error!, wasRunning, stopRequested, request.SourceName, warnings)
                    .ConfigureAwait(false);
            }

            InstanceMetadata target = new()
            {
                Id = id,
                Name = request.TargetName,
                CreatedUtc = now,
                DefaultUser = sourceMetadata.IsSuccess
                    ? sourceMetadata.Value.DefaultUser
                    : InstanceMetadata.DefaultUserName,
                DockerDesktop = sourceMetadata.IsSuccess
                    ? sourceMetadata.Value.DockerDesktop
                    : InstanceMetadata.DockerUnknown,
                Appearance = sourceMetadata.IsSuccess ? sourceMetadata.Value.Appearance : null,
                Source = new InstanceSource
                {
                    Kind = "Duplicate",
                    Reference = request.SourceName,
                },
            };

            OperationResult saved = _metadata.Save(targetPath, target);

            if (!saved.IsSuccess)
            {
                return await FailWithRollbackAsync(transaction, saved.Error!, wasRunning, stopRequested, request.SourceName, warnings)
                    .ConfigureAwait(false);
            }

            CopyIcon(sourcePath, targetPath);
            _ = await TryRestartAsync(wasRunning, stopRequested, request.SourceName, warnings)
                .ConfigureAwait(false);

            DuplicateOutcome outcome = new(request.SourceName, request.TargetName, targetPath);

            return warnings.Count == 0
                ? OperationResult<DuplicateOutcome>.Ok(outcome)
                : OperationResult<DuplicateOutcome>.Warn(outcome, [.. warnings]);
        }
        catch (OperationCanceledException)
        {
            IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);
            _ = await TryRestartAsync(wasRunning, stopRequested, request.SourceName, warnings)
                .ConfigureAwait(false);

            return OperationResult<DuplicateOutcome>.Fail(
                ErrorCode.Cancelled,
                "Duplicate cancelled.",
                OperationFailure.Combine(null, [.. warnings, .. rollbackFailures]));
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);
            _ = await TryRestartAsync(wasRunning, stopRequested, request.SourceName, warnings)
                .ConfigureAwait(false);

            return OperationFailure.From<DuplicateOutcome>(exception, [.. warnings, .. rollbackFailures]);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
        }
    }

    private async Task<OperationResult<DuplicateOutcome>> FailWithRollbackAsync(
        OperationTransaction transaction,
        OperationError error,
        bool wasRunning,
        bool stopRequested,
        string sourceName,
        List<string> warnings)
    {
        IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);
        _ = await TryRestartAsync(wasRunning, stopRequested, sourceName, warnings).ConfigureAwait(false);

        return OperationFailure.From<DuplicateOutcome>(error, [.. warnings, .. rollbackFailures]);
    }

    private async Task<bool> TryRestartAsync(
        bool wasRunning,
        bool stopRequested,
        string sourceName,
        List<string> warnings)
    {
        if (!wasRunning || !stopRequested)
        {
            return false;
        }

        try
        {
            await _wsl.StartDistributionAsync(sourceName, CancellationToken.None).ConfigureAwait(false);

            return true;
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            warnings.Add($"restart: {exception.Message}");

            return false;
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
                    $"Not enough space to duplicate: {requiredBytes} bytes needed.")
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

    private void CopyIcon(string sourcePath, string targetPath)
    {
        string source = _fileSystem.Path.Combine(sourcePath, "terminal-icon.png");

        if (!_fileSystem.File.Exists(source))
        {
            return;
        }

        try
        {
            _fileSystem.File.Copy(source, _fileSystem.Path.Combine(targetPath, "terminal-icon.png"), true);
        }
        catch (IOException)
        {
            // Icons are cosmetic.
        }
        catch (UnauthorizedAccessException)
        {
            // Icons are cosmetic.
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
