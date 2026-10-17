using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Compacts the virtual disk of an instance, restarting it when needed.</summary>
public sealed class CompactHandler
{
    private readonly InstanceStateService _instances;
    private readonly IWslGateway _wsl;
    private readonly ArchiveService _archiveService;

    public CompactHandler(InstanceStateService instances, IWslGateway wsl, ArchiveService archiveService)
    {
        _instances = instances;
        _wsl = wsl;
        _archiveService = archiveService;
    }

    public async Task<OperationResult<CompactOutcome>> CompactAsync(
        string fleetRoot,
        CompactRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, request.InstanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<CompactOutcome>(managed.Error!);
        }

        OperationResult<bool> running = await _instances
            .IsRunningAsync(request.InstanceName, cancellationToken)
            .ConfigureAwait(false);

        if (!running.IsSuccess)
        {
            return OperationFailure.From<CompactOutcome>(running.Error!);
        }

        bool wasRunning = running.Value;
        List<string> warnings = [];

        if (request.ArchiveFirst)
        {
            OperationResult<ArchiveOutcome> archived = await _archiveService
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

            if (!archived.IsSuccess)
            {
                return OperationFailure.From<CompactOutcome>(archived.Error!);
            }
        }

        long sizeBefore = _instances.GetVirtualDiskSize(managed.Value);

        if (wasRunning)
        {
            try
            {
                await _wsl.TerminateDistributionAsync(request.InstanceName, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (OperationFailure.IsExpected(exception))
            {
                warnings.Add($"terminate: {exception.Message}");
            }
        }

        try
        {
            await _wsl.CompactDistributionAsync(request.InstanceName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            _ = await RestartAsync(wasRunning, request.InstanceName, warnings).ConfigureAwait(false);

            return OperationFailure.From<CompactOutcome>(exception, warnings);
        }

        long sizeAfter = _instances.GetVirtualDiskSize(managed.Value);
        bool restarted = await RestartAsync(wasRunning, request.InstanceName, warnings).ConfigureAwait(false);

        CompactOutcome outcome = new(
            request.InstanceName,
            sizeBefore,
            sizeAfter,
            sizeBefore - sizeAfter,
            restarted);

        return warnings.Count == 0
            ? OperationResult<CompactOutcome>.Ok(outcome)
            : OperationResult<CompactOutcome>.Warn(outcome, [.. warnings]);
    }

    private async Task<bool> RestartAsync(bool wasRunning, string instanceName, List<string> warnings)
    {
        if (!wasRunning)
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
}
