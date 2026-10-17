using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Optionally archives an instance, then unregisters and deletes it.</summary>
public sealed class RemoveInstanceHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly InstanceStateService _instances;
    private readonly IWslGateway _wsl;
    private readonly IWslRegistryGateway _registry;
    private readonly ArchiveService _archiveService;

    public RemoveInstanceHandler(
        IFileSystem fileSystem,
        InstanceStateService instances,
        IWslGateway wsl,
        IWslRegistryGateway registry,
        ArchiveService archiveService)
    {
        _fileSystem = fileSystem;
        _instances = instances;
        _wsl = wsl;
        _registry = registry;
        _archiveService = archiveService;
    }

    public async Task<OperationResult<RemoveOutcome>> RemoveAsync(
        string fleetRoot,
        RemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, request.InstanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<RemoveOutcome>(managed.Error!);
        }

        string installPath = managed.Value;
        bool archived = false;

        if (request.ArchiveFirst)
        {
            OperationResult<ArchiveOutcome> archivedResult = await _archiveService
                .ArchiveAsync(
                    fleetRoot,
                    new ArchiveRequest(
                        request.InstanceName,
                        null,
                        ArchiveFormat.Default,
                        Replace: true,
                        StopIfRunning: true),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!archivedResult.IsSuccess)
            {
                return OperationFailure.From<RemoveOutcome>(archivedResult.Error!);
            }

            archived = true;
        }

        try
        {
            await _wsl.TerminateDistributionAsync(request.InstanceName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            // An already stopped instance needs no termination.
        }

        if (IsRegistered(request.InstanceName))
        {
            try
            {
                await _wsl.UnregisterDistributionAsync(request.InstanceName, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (OperationFailure.IsExpected(exception))
            {
                return OperationFailure.From<RemoveOutcome>(exception);
            }
        }

        List<string> warnings = [];
        TryDeleteDirectory(installPath, warnings);

        RemoveOutcome outcome = new(request.InstanceName, archived, warnings);

        return warnings.Count == 0
            ? OperationResult<RemoveOutcome>.Ok(outcome)
            : OperationResult<RemoveOutcome>.Warn(outcome, [.. warnings]);
    }

    private bool IsRegistered(string name)
    {
        if (!_registry.IsSupported)
        {
            return true;
        }

        try
        {
            return _registry.GetDistributions()
                .Any(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private void TryDeleteDirectory(string path, List<string> warnings)
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
            warnings.Add($"cleanup: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            warnings.Add($"cleanup: {exception.Message}");
        }
    }
}
