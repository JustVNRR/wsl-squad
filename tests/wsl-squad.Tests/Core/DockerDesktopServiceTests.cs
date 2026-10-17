using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Docker;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core;

public class DockerDesktopServiceTests
{
    private readonly LifecycleFixture _fixture = new();
    private readonly FakeDockerDesktopStore _store = new();
    private readonly FakeProcessRunner _processRunner = new();

    private DockerDesktopService CreateService()
    {
        return new DockerDesktopService(_store, _fixture.Instances, _processRunner);
    }

    [Fact]
    public async Task Registers_an_instance_and_restarts_docker_desktop()
    {
        _fixture.AddInstance();

        OperationResult<DockerIntegrationOutcome> result = await CreateService()
            .RegisterAsync(LifecycleFixture.Root, "demo", restart: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.State.Should().Be("yes");
        result.Value.Restarted.Should().BeTrue();
        _store.IntegratedDistros.Should().Equal("demo");
        _processRunner.Invocations.Should().ContainSingle();
        _processRunner.Invocations[0].FileName.Should().Be("docker");
        _processRunner.Invocations[0].Arguments.Should().Equal("desktop", "restart");
    }

    [Fact]
    public async Task Does_nothing_when_already_integrated()
    {
        _fixture.AddInstance();
        _store.SetIntegrated("demo");

        OperationResult<DockerIntegrationOutcome> result = await CreateService()
            .RegisterAsync(LifecycleFixture.Root, "demo", restart: true);

        result.Value.State.Should().Be("yes");
        result.Value.Restarted.Should().BeFalse();
        _store.WriteCount.Should().Be(0);
        _processRunner.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Unregisters_an_instance()
    {
        _fixture.AddInstance();
        _store.SetIntegrated("demo");

        OperationResult<DockerIntegrationOutcome> result = await CreateService()
            .UnregisterAsync(LifecycleFixture.Root, "demo", restart: false);

        result.Value.State.Should().Be("no");
        _store.IntegratedDistros.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_when_docker_desktop_settings_are_absent()
    {
        _fixture.AddInstance();
        _store.SettingsAvailable = false;

        OperationResult<DockerIntegrationOutcome> result = await CreateService()
            .RegisterAsync(LifecycleFixture.Root, "demo", restart: false);

        result.Error!.Code.Should().Be(ErrorCode.InvalidState);
    }

    [Fact]
    public async Task Reports_a_restart_failure_as_a_warning()
    {
        _fixture.AddInstance();
        _processRunner.Enqueue(new ProcessResult(1, string.Empty, "restart failed"));

        OperationResult<DockerIntegrationOutcome> result = await CreateService()
            .RegisterAsync(LifecycleFixture.Root, "demo", restart: true);

        result.Status.Should().Be(OperationStatus.Warning);
        result.Value.Restarted.Should().BeFalse();
        result.Value.State.Should().Be("yes");
        result.Warnings.Should().Contain(warning => warning.Contains("restart", StringComparison.Ordinal));
    }
}
