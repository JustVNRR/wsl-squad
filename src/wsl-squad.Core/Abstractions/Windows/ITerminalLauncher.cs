namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Opens a shell for an instance, using a Windows Terminal profile when possible.</summary>
public interface ITerminalLauncher
{
    public bool TryLaunchProfile(string instanceName);

    public void LaunchShell(string instanceName);
}
