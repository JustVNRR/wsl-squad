using System.ComponentModel;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Terminates and reopens an instance.</summary>
public sealed class RestartHandler
{
    private readonly InstanceStateService _instances;
    private readonly IWslGateway _wsl;
    private readonly ITerminalLauncher _terminal;

    public RestartHandler(InstanceStateService instances, IWslGateway wsl, ITerminalLauncher terminal)
    {
        _instances = instances;
        _wsl = wsl;
        _terminal = terminal;
    }

    public async Task<OperationResult<RestartOutcome>> RestartAsync(
        string fleetRoot,
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<RestartOutcome>(managed.Error!);
        }

        try
        {
            await _wsl.TerminateDistributionAsync(instanceName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            // An already stopped instance needs no termination.
        }

        try
        {
            bool usedProfile = _terminal.TryLaunchProfile(instanceName);

            if (!usedProfile)
            {
                _terminal.LaunchShell(instanceName);
            }

            return OperationResult<RestartOutcome>.Ok(new RestartOutcome(instanceName, usedProfile));
        }
        catch (Win32Exception exception)
        {
            return OperationResult<RestartOutcome>.Fail(
                ErrorCode.ExternalTool,
                "The shell could not be started.",
                exception.Message);
        }
    }
}
