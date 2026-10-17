using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Tests.Support;

public sealed class FakeTerminalLauncher : ITerminalLauncher
{
    private readonly List<string> _profiles = [];
    private readonly List<string> _shells = [];

    public bool ProfileAvailable { get; set; } = true;

    public Exception? LaunchFailure { get; set; }

    public IReadOnlyList<string> Profiles => _profiles;

    public IReadOnlyList<string> Shells => _shells;

    public bool TryLaunchProfile(string instanceName)
    {
        _profiles.Add(instanceName);

        return ProfileAvailable;
    }

    public void LaunchShell(string instanceName)
    {
        _shells.Add(instanceName);

        if (LaunchFailure is not null)
        {
            throw LaunchFailure;
        }
    }
}
