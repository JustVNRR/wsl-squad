using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Mappers;

namespace wsl_squad.Presentation.Presenters.Fleet;

/// <summary>Loads the fleet snapshot for the list command and the CLI menu.</summary>
public sealed class FleetListPresenter
{
    private readonly FleetRootResolver _rootResolver;
    private readonly FleetDiscoveryService _discovery;

    public FleetListPresenter(FleetRootResolver rootResolver, FleetDiscoveryService discovery)
    {
        _rootResolver = rootResolver;
        _discovery = discovery;
    }

    public async Task<OperationResult<FleetSnapshotDto>> LoadAsync(
        bool includeArchives,
        CancellationToken cancellationToken = default)
    {
        OperationResult<string> root = _rootResolver.Resolve();

        if (!root.IsSuccess)
        {
            return Fail(root.Error!);
        }

        OperationResult<FleetSnapshot> snapshot = await _discovery
            .GetSnapshotAsync(root.Value, includeArchives, cancellationToken)
            .ConfigureAwait(false);

        if (!snapshot.IsSuccess)
        {
            return Fail(snapshot.Error!);
        }

        return OperationResult<FleetSnapshotDto>.Ok(snapshot.Value.ToDto());
    }

    private static OperationResult<FleetSnapshotDto> Fail(OperationError error)
    {
        return OperationResult<FleetSnapshotDto>.Fail(
            error.Code,
            error.Message,
            error.Detail,
            error.ExternalExitCode);
    }
}
