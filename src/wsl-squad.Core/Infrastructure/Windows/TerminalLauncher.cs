using System.ComponentModel;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Opens Windows Terminal profiles, falling back to a plain WSL shell.</summary>
public sealed class TerminalLauncher : ITerminalLauncher
{
    private readonly IProcessLauncher _processLauncher;

    public TerminalLauncher(IProcessLauncher processLauncher)
    {
        _processLauncher = processLauncher;
    }

    public bool TryLaunchProfile(string instanceName)
    {
        try
        {
            _processLauncher.Start(TerminalCommandBuilder.Profile(instanceName));

            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    public void LaunchShell(string instanceName)
    {
        _processLauncher.Start(TerminalCommandBuilder.Shell(instanceName));
    }
}
