using System.ComponentModel;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Opens a shell for an instance.</summary>
public sealed class OpenHandler
{
    private readonly InstanceStateService _instances;
    private readonly ITerminalLauncher _terminal;

    public OpenHandler(InstanceStateService instances, ITerminalLauncher terminal)
    {
        _instances = instances;
        _terminal = terminal;
    }

    public OperationResult<OpenOutcome> Open(string fleetRoot, string instanceName)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<OpenOutcome>(managed.Error!);
        }

        try
        {
            bool usedProfile = _terminal.TryLaunchProfile(instanceName);

            if (!usedProfile)
            {
                _terminal.LaunchShell(instanceName);
            }

            return OperationResult<OpenOutcome>.Ok(new OpenOutcome(instanceName, usedProfile));
        }
        catch (Win32Exception exception)
        {
            return OperationResult<OpenOutcome>.Fail(
                ErrorCode.ExternalTool,
                "The shell could not be started.",
                exception.Message);
        }
    }
}
