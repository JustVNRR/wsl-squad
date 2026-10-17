using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class RemoveInstanceHandlerTests
{
    private readonly LifecycleFixture _fixture = new();

    private RemoveInstanceHandler CreateHandler()
    {
        return new RemoveInstanceHandler(
            _fixture.FileSystem,
            _fixture.Instances,
            _fixture.Wsl,
            _fixture.Registry,
            _fixture.CreateArchiveService());
    }

    [Fact]
    public async Task Removes_a_registered_instance_and_its_folder()
    {
        _fixture.AddInstance();
        _fixture.Registry.Add("demo", "/fleet/demo");

        OperationResult<RemoveOutcome> result = await CreateHandler()
            .RemoveAsync(LifecycleFixture.Root, new RemoveRequest("demo", ArchiveFirst: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Archived.Should().BeFalse();
        _fixture.Wsl.UnregisteredNames.Should().Equal("demo");
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeFalse();
    }

    [Fact]
    public async Task Stops_when_the_archive_fails_before_unregistering()
    {
        _fixture.AddInstance();
        _fixture.Registry.Add("demo", "/fleet/demo");
        _fixture.Wsl.ExportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--export"]),
            new ProcessResult(1, string.Empty, "export failed"));

        OperationResult<RemoveOutcome> result = await CreateHandler()
            .RemoveAsync(LifecycleFixture.Root, new RemoveRequest("demo", ArchiveFirst: true));

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeTrue();
        _fixture.Wsl.UnregisteredNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Deletes_an_unregistered_orphan_folder_without_unregistering()
    {
        _fixture.AddInstance();

        OperationResult<RemoveOutcome> result = await CreateHandler()
            .RemoveAsync(LifecycleFixture.Root, new RemoveRequest("demo", ArchiveFirst: false));

        result.IsSuccess.Should().BeTrue();
        _fixture.Wsl.UnregisteredNames.Should().BeEmpty();
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeFalse();
    }

    [Fact]
    public async Task Archives_before_removing()
    {
        _fixture.AddInstance();
        _fixture.Registry.Add("demo", "/fleet/demo");
        _fixture.ExportWritesArchive();

        OperationResult<RemoveOutcome> result = await CreateHandler()
            .RemoveAsync(LifecycleFixture.Root, new RemoveRequest("demo", ArchiveFirst: true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Archived.Should().BeTrue();
        _fixture.FileSystem.File.Exists("/fleet/archives/demo_archive/archive.json").Should().BeTrue();
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeFalse();
    }
}
