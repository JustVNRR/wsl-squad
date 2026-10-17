using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class InstanceStateServiceTests
{
    private readonly LifecycleFixture _fixture = new();

    [Fact]
    public void Returns_the_path_of_a_managed_instance()
    {
        _fixture.AddInstance();

        _fixture.Instances.GetManagedPath(LifecycleFixture.Root, "demo").Value.Should().Be("/fleet/demo");
    }

    [Fact]
    public void Rejects_an_invalid_name()
    {
        OperationResult<string> result = _fixture.Instances.GetManagedPath(LifecycleFixture.Root, "bad name");

        result.Error!.Code.Should().Be(ErrorCode.InvalidName);
    }

    [Fact]
    public void Rejects_a_missing_instance()
    {
        OperationResult<string> result = _fixture.Instances.GetManagedPath(LifecycleFixture.Root, "demo");

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Rejects_an_unmanaged_folder()
    {
        _fixture.FileSystem.AddFile("/fleet/demo/file.txt", new MockFileData("data"));

        OperationResult<string> result = _fixture.Instances.GetManagedPath(LifecycleFixture.Root, "demo");

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Reports_the_running_state()
    {
        _fixture.AddInstance(running: true);

        (await _fixture.Instances.IsRunningAsync("demo")).Value.Should().BeTrue();
        (await _fixture.Instances.IsRunningAsync("other")).Value.Should().BeFalse();
    }

    [Fact]
    public async Task Reports_wsl_failures_as_external_tool_errors()
    {
        _fixture.Wsl.Failure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--list"]),
            new ProcessResult(1, string.Empty, "failed"));

        OperationResult<bool> result = await _fixture.Instances.IsRunningAsync("demo");

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
    }

    [Fact]
    public void Reports_the_virtual_disk_size()
    {
        _fixture.AddInstance();

        _fixture.Instances.GetVirtualDiskSize("/fleet/demo").Should().Be(10);
    }
}
