using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Docker;

namespace wsl_squad.Core.Application.Docker;

/// <summary>Outcome of a Docker Desktop integration change.</summary>
public sealed record DockerIntegrationOutcome(string InstanceName, string State, bool Restarted);

/// <summary>Registers or unregisters an instance with Docker Desktop.</summary>
public sealed class DockerDesktopService
{
    private readonly IDockerDesktopStore _store;
    private readonly InstanceStateService _instances;
    private readonly IProcessRunner _processRunner;

    public DockerDesktopService(
        IDockerDesktopStore store,
        InstanceStateService instances,
        IProcessRunner processRunner)
    {
        _store = store;
        _instances = instances;
        _processRunner = processRunner;
    }

    public Task<OperationResult<DockerIntegrationOutcome>> RegisterAsync(
        string fleetRoot,
        string instanceName,
        bool restart,
        CancellationToken cancellationToken = default)
    {
        return SetIntegrationAsync(fleetRoot, instanceName, integrated: true, restart, cancellationToken);
    }

    public Task<OperationResult<DockerIntegrationOutcome>> UnregisterAsync(
        string fleetRoot,
        string instanceName,
        bool restart,
        CancellationToken cancellationToken = default)
    {
        return SetIntegrationAsync(fleetRoot, instanceName, integrated: false, restart, cancellationToken);
    }

    private async Task<OperationResult<DockerIntegrationOutcome>> SetIntegrationAsync(
        string fleetRoot,
        string instanceName,
        bool integrated,
        bool restart,
        CancellationToken cancellationToken)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<DockerIntegrationOutcome>(managed.Error!);
        }

        OperationResult<DockerDesktopSettings> settings = _store.Read();

        if (!settings.IsSuccess)
        {
            return OperationFailure.From<DockerIntegrationOutcome>(settings.Error!);
        }

        if (!settings.Value.SettingsAvailable)
        {
            return OperationResult<DockerIntegrationOutcome>.Fail(
                ErrorCode.InvalidState,
                "Docker Desktop settings were not found. Start Docker Desktop and retry.");
        }

        bool currentlyIntegrated = settings.Value.IntegratedDistros.Contains(
            instanceName,
            StringComparer.OrdinalIgnoreCase);

        if (currentlyIntegrated == integrated)
        {
            return OperationResult<DockerIntegrationOutcome>.Ok(
                new DockerIntegrationOutcome(instanceName, integrated ? "yes" : "no", Restarted: false));
        }

        OperationResult write = _store.SetIntegration(instanceName, integrated);

        if (!write.IsSuccess)
        {
            return OperationFailure.From<DockerIntegrationOutcome>(write.Error!);
        }

        List<string> warnings = [];
        bool restarted = false;

        if (restart)
        {
            try
            {
                ProcessInvocation invocation = DockerCommandBuilder.RestartDesktop();
                ProcessResult result = await _processRunner
                    .RunAsync(invocation, cancellationToken)
                    .ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    throw new ProcessExecutionException(invocation, result);
                }

                restarted = true;
            }
            catch (Exception exception) when (OperationFailure.IsExpected(exception))
            {
                warnings.Add($"Docker Desktop restart: {exception.Message}");
            }
        }

        DockerIntegrationOutcome outcome = new(instanceName, integrated ? "yes" : "no", restarted);

        return warnings.Count == 0
            ? OperationResult<DockerIntegrationOutcome>.Ok(outcome)
            : OperationResult<DockerIntegrationOutcome>.Warn(outcome, [.. warnings]);
    }
}
