using System.ComponentModel;

using FluentAssertions;

using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class OpenRestartHandlerTests
{
    private readonly LifecycleFixture _fixture = new();
    private readonly FakeTerminalLauncher _terminal = new();

    [Fact]
    public void Open_uses_the_terminal_profile_when_available()
    {
        _fixture.AddInstance();

        OperationResult<OpenOutcome> result = new OpenHandler(_fixture.Instances, _terminal)
            .Open(LifecycleFixture.Root, "demo");

        result.IsSuccess.Should().BeTrue();
        result.Value.UsedTerminalProfile.Should().BeTrue();
        _terminal.Profiles.Should().Equal("demo");
        _terminal.Shells.Should().BeEmpty();
    }

    [Fact]
    public void Open_falls_back_to_a_plain_shell()
    {
        _fixture.AddInstance();
        _terminal.ProfileAvailable = false;

        OperationResult<OpenOutcome> result = new OpenHandler(_fixture.Instances, _terminal)
            .Open(LifecycleFixture.Root, "demo");

        result.Value.UsedTerminalProfile.Should().BeFalse();
        _terminal.Shells.Should().Equal("demo");
    }

    [Fact]
    public async Task Restart_terminates_and_launches_the_profile()
    {
        _fixture.AddInstance(running: true);

        OperationResult<RestartOutcome> result = await new RestartHandler(_fixture.Instances, _fixture.Wsl, _terminal)
            .RestartAsync(LifecycleFixture.Root, "demo");

        result.IsSuccess.Should().BeTrue();
        _fixture.Wsl.TerminatedNames.Should().Equal("demo");
        _terminal.Profiles.Should().Equal("demo");
    }

    [Fact]
    public async Task Restart_reports_a_shell_launch_failure()
    {
        _fixture.AddInstance();
        _terminal.ProfileAvailable = false;
        _terminal.LaunchFailure = new Win32Exception("wsl.exe not found");

        OperationResult<RestartOutcome> result = await new RestartHandler(_fixture.Instances, _fixture.Wsl, _terminal)
            .RestartAsync(LifecycleFixture.Root, "demo");

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
    }
}
