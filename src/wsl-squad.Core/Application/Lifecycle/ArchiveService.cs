using System.IO.Abstractions;
using System.Security.Cryptography;

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

/// <summary>Exports an instance into an archive folder with metadata and checksum.</summary>
public sealed class ArchiveService
{
    private const long _freeSpaceMargin = 64L * 1024 * 1024;

    private readonly IFileSystem _fileSystem;
    private readonly InstanceStateService _instances;
    private readonly IWslGateway _wsl;
    private readonly ArchiveStore _archives;
    private readonly InstanceMetadataStore _metadata;
    private readonly IDiskSpaceProbe _diskSpace;
    private readonly IClock _clock;

    public ArchiveService(
        IFileSystem fileSystem,
        InstanceStateService instances,
        IWslGateway wsl,
        ArchiveStore archives,
        InstanceMetadataStore metadata,
        IDiskSpaceProbe diskSpace,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _instances = instances;
        _wsl = wsl;
        _archives = archives;
        _metadata = metadata;
        _diskSpace = diskSpace;
        _clock = clock;
    }

    public async Task<OperationResult<ArchiveOutcome>> ArchiveAsync(
        string fleetRoot,
        ArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string archiveName = string.IsNullOrWhiteSpace(request.ArchiveName)
            ? $"{request.InstanceName}_archive"
            : request.ArchiveName;

        if (!InstanceName.IsValid(archiveName))
        {
            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.InvalidName,
                $"Archive name '{archiveName}' is invalid.");
        }

        if (!ArchiveFormat.IsSupported(request.Format))
        {
            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.Validation,
                $"Archive format '{request.Format}' is not supported.");
        }

        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, request.InstanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<ArchiveOutcome>(managed.Error!);
        }

        string installPath = managed.Value;
        string archivesRoot = _archives.GetArchivesRoot(fleetRoot);
        string archiveDirectory = _archives.GetArchiveDirectory(fleetRoot, archiveName);

        if (!PathHelper.IsUnder(archiveDirectory, archivesRoot))
        {
            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.Validation,
                $"Archive path '{archiveDirectory}' is outside the archives root.");
        }

        bool replacing = _fileSystem.Directory.Exists(archiveDirectory);

        if (replacing && !request.Replace)
        {
            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.AlreadyExists,
                $"Archive '{archiveName}' already exists. Use --replace to overwrite it.");
        }

        OperationResult<bool> running = await _instances
            .IsRunningAsync(request.InstanceName, cancellationToken)
            .ConfigureAwait(false);

        if (!running.IsSuccess)
        {
            return OperationFailure.From<ArchiveOutcome>(running.Error!);
        }

        bool wasRunning = running.Value;

        if (wasRunning && !request.StopIfRunning)
        {
            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.ConfirmationRequired,
                $"Instance '{request.InstanceName}' is running. Use --stop to archive it anyway.");
        }

        long virtualDiskSize = _instances.GetVirtualDiskSize(installPath);
        OperationResult space = CheckFreeSpace(archivesRoot, virtualDiskSize);

        if (!space.IsSuccess)
        {
            return OperationFailure.From<ArchiveOutcome>(space.Error!);
        }

        string operationId = Guid.NewGuid().ToString("N");
        string stagingRoot = _fileSystem.Path.Combine(fleetRoot, ".staging", operationId);
        string stagedArchiveDirectory = _fileSystem.Path.Combine(stagingRoot, archiveName);
        string tarPath = _fileSystem.Path.Combine(stagedArchiveDirectory, $"{archiveName}.{request.Format}");
        List<string> warnings = [];
        bool stopRequested = false;

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(stagedArchiveDirectory);

            if (wasRunning)
            {
                await _wsl.TerminateDistributionAsync(request.InstanceName, cancellationToken)
                    .ConfigureAwait(false);
                stopRequested = true;
            }

            await _wsl.ExportDistributionAsync(request.InstanceName, tarPath, request.Format, cancellationToken)
                .ConfigureAwait(false);

            long sizeBytes = _instances.GetFileSize(tarPath);

            if (sizeBytes <= 0)
            {
                return await FailAsync(
                    new OperationError(ErrorCode.IoError, "The exported archive is empty."),
                    wasRunning,
                    stopRequested,
                    request.InstanceName,
                    warnings).ConfigureAwait(false);
            }

            ArchiveMetadata metadata = BuildMetadata(request, archiveName, tarPath, sizeBytes, installPath);
            OperationResult write = _archives.Write(stagedArchiveDirectory, metadata);

            if (!write.IsSuccess)
            {
                return await FailAsync(write.Error!, wasRunning, stopRequested, request.InstanceName, warnings)
                    .ConfigureAwait(false);
            }

            CopyIcon(installPath, stagedArchiveDirectory, warnings);

            CommitArchive(archiveDirectory, stagedArchiveDirectory, archivesRoot, archiveName, operationId, warnings);

            bool restarted = await TryRestartAsync(
                wasRunning,
                stopRequested,
                request.InstanceName,
                warnings).ConfigureAwait(false);

            string finalTarPath = _fileSystem.Path.Combine(archiveDirectory, metadata.File);
            ArchiveOutcome outcome = new(
                request.InstanceName,
                archiveName,
                archiveDirectory,
                finalTarPath,
                request.Format,
                sizeBytes,
                restarted);

            return warnings.Count == 0
                ? OperationResult<ArchiveOutcome>.Ok(outcome)
                : OperationResult<ArchiveOutcome>.Warn(outcome, [.. warnings]);
        }
        catch (OperationCanceledException)
        {
            warnings.Add("operation cancelled");
            _ = await TryRestartAsync(wasRunning, stopRequested, request.InstanceName, warnings).ConfigureAwait(false);

            return OperationResult<ArchiveOutcome>.Fail(
                ErrorCode.Cancelled,
                "Archive cancelled.",
                OperationFailure.Combine(null, warnings));
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            _ = await TryRestartAsync(wasRunning, stopRequested, request.InstanceName, warnings).ConfigureAwait(false);

            return OperationFailure.From<ArchiveOutcome>(exception, warnings);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
        }
    }

    private ArchiveMetadata BuildMetadata(
        ArchiveRequest request,
        string archiveName,
        string tarPath,
        long sizeBytes,
        string installPath)
    {
        OperationResult<InstanceMetadata> instanceMetadata = _metadata.Read(installPath);

        return new ArchiveMetadata
        {
            Name = archiveName,
            SourceInstance = request.InstanceName,
            CreatedUtc = _clock.UtcNow,
            Format = request.Format,
            File = _fileSystem.Path.GetFileName(tarPath),
            SizeBytes = sizeBytes,
            Sha256 = ComputeSha256(tarPath),
            DefaultUser = instanceMetadata.IsSuccess
                ? instanceMetadata.Value.DefaultUser
                : InstanceMetadata.DefaultUserName,
            DockerDesktop = instanceMetadata.IsSuccess
                ? instanceMetadata.Value.DockerDesktop
                : InstanceMetadata.DockerUnknown,
            Appearance = instanceMetadata.IsSuccess ? instanceMetadata.Value.Appearance : null,
        };
    }

    private void CommitArchive(
        string archiveDirectory,
        string stagedArchiveDirectory,
        string archivesRoot,
        string archiveName,
        string operationId,
        List<string> warnings)
    {
        string? backup = null;

        if (_fileSystem.Directory.Exists(archiveDirectory))
        {
            backup = _fileSystem.Path.Combine(archivesRoot, $".old-{archiveName}-{operationId}");
            _fileSystem.Directory.Move(archiveDirectory, backup);
        }

        try
        {
            _fileSystem.Directory.Move(stagedArchiveDirectory, archiveDirectory);
        }
        catch
        {
            if (backup is not null && !_fileSystem.Directory.Exists(archiveDirectory))
            {
                _fileSystem.Directory.Move(backup, archiveDirectory);
            }

            throw;
        }

        if (backup is not null)
        {
            TryDeleteDirectory(backup, warnings);
        }
    }

    private async Task<bool> TryRestartAsync(
        bool wasRunning,
        bool stopRequested,
        string instanceName,
        List<string> warnings)
    {
        if (!wasRunning || !stopRequested)
        {
            return false;
        }

        try
        {
            await _wsl.StartDistributionAsync(instanceName, CancellationToken.None).ConfigureAwait(false);

            return true;
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            warnings.Add($"restart: {exception.Message}");

            return false;
        }
    }

    private async Task<OperationResult<ArchiveOutcome>> FailAsync(
        OperationError error,
        bool wasRunning,
        bool stopRequested,
        string instanceName,
        List<string> details)
    {
        _ = await TryRestartAsync(wasRunning, stopRequested, instanceName, details).ConfigureAwait(false);

        return OperationFailure.From<ArchiveOutcome>(error, details);
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
                    $"Not enough space to archive: {requiredBytes} bytes needed.")
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

    private void CopyIcon(string installPath, string archiveDirectory, List<string> warnings)
    {
        string source = _fileSystem.Path.Combine(installPath, "terminal-icon.png");

        if (!_fileSystem.File.Exists(source))
        {
            return;
        }

        try
        {
            _fileSystem.File.Copy(source, _fileSystem.Path.Combine(archiveDirectory, "terminal-icon.png"), true);
        }
        catch (IOException exception)
        {
            warnings.Add($"icon: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            warnings.Add($"icon: {exception.Message}");
        }
    }

    private string ComputeSha256(string path)
    {
        using Stream stream = _fileSystem.File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private void TryDeleteDirectory(string path, List<string>? warnings = null)
    {
        try
        {
            if (_fileSystem.Directory.Exists(path))
            {
                _fileSystem.Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException exception)
        {
            warnings?.Add($"cleanup: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            warnings?.Add($"cleanup: {exception.Message}");
        }
    }
}
