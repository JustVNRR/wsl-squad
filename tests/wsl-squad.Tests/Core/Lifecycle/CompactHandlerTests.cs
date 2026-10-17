using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class CompactHandlerTests
{
    private readonly LifecycleFixture _fixture = new();

    private CompactHandler CreateHandler()
    {
        return new CompactHandler(_fixture.Instances, _fixture.Wsl, _fixture.CreateArchiveService());
    }

    private void ShrinkVirtualDiskOnCompact()
    {
        _fixture.Wsl.CompactAction = name =>
            _fixture.FileSystem.File.WriteAllText($"/fleet/{name}/ext4.vhdx", "0");
    }

    [Fact]
    public async Task Compacts_and_restarts_a_running_instance()
    {
        _fixture.AddInstance(running: true);
        ShrinkVirtualDiskOnCompact();

        OperationResult<CompactOutcome> result = await CreateHandler()
            .CompactAsync(LifecycleFixture.Root, new CompactRequest("demo", ArchiveFirst: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.SizeBefore.Should().Be(10);
        result.Value.SizeAfter.Should().Be(1);
        result.Value.Gained.Should().Be(9);
        result.Value.Restarted.Should().BeTrue();
        _fixture.Wsl.TerminatedNames.Should().Equal("demo");
        _fixture.Wsl.Compacts.Should().Equal("demo");
        _fixture.Wsl.Starts.Should().Equal("demo");
    }

    [Fact]
    public async Task Leaves_a_stopped_instance_stopped()
    {
        _fixture.AddInstance();
        ShrinkVirtualDiskOnCompact();

        OperationResult<CompactOutcome> result = await CreateHandler()
            .CompactAsync(LifecycleFixture.Root, new CompactRequest("demo", ArchiveFirst: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Restarted.Should().BeFalse();
        _fixture.Wsl.TerminatedNames.Should().BeEmpty();
        _fixture.Wsl.Starts.Should().BeEmpty();
    }

    [Fact]
    public async Task Restarts_the_instance_when_compaction_fails()
    {
        _fixture.AddInstance(running: true);
        _fixture.Wsl.CompactFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--manage", "demo", "--compact"]),
            new ProcessResult(1, string.Empty, "compact failed"));

        OperationResult<CompactOutcome> result = await CreateHandler()
            .CompactAsync(LifecycleFixture.Root, new CompactRequest("demo", ArchiveFirst: false));

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.Wsl.Starts.Should().Equal("demo");
    }
}
