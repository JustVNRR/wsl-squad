using FluentAssertions;

using wsl_squad.Windows.Tests.Support;

namespace wsl_squad.Windows.Tests;

public class WindowsEnvironmentTests
{
    [WindowsFact]
    public void Runs_on_windows()
    {
        OperatingSystem.IsWindows().Should().BeTrue();
    }
}
