using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class DuplicateHandlerTests
{
    private readonly LifecycleFixture _fixture = new();

    private DuplicateHandler CreateHandler()
    {
        return new DuplicateHandler(
            _fixture.FileSystem,
            _fixture.Instances,
            _fixture.Registry,
            _fixture.Wsl,
            _fixture.Markers,
            _fixture.Metadata,
            _fixture.Archives,
            _fixture.DiskSpace,
            _fixture.Clock);
    }

    [Fact]
    public async Task Duplicates_an_instance_and_restarts_the_source()
    {
        _fixture.AddInstance(running: true, defaultUser: "jane", docker: "yes");

        OperationResult<DuplicateOutcome> result = await CreateHandler()
            .DuplicateAsync(LifecycleFixture.Root, new DuplicateRequest("demo", "copy"));

        result.IsSuccess.Should().BeTrue();
        _fixture.Wsl.TerminatedNames.Should().Contain("demo");
        _fixture.Wsl.ImportedNames.Should().Equal("copy");
        _fixture.Wsl.Starts.Should().Equal("demo");
        _fixture.FileSystem.File.Exists("/fleet/copy/.wsl-squad").Should().BeTrue();

        OperationResult<InstanceMetadata> metadata = _fixture.Metadata.Read("/fleet/copy");
        metadata.Value.DefaultUser.Should().Be("jane");
        metadata.Value.Source!.Kind.Should().Be("Duplicate");
        metadata.Value.Source.Reference.Should().Be("demo");
    }

    [Fact]
    public async Task Rejects_an_existing_target()
    {
        _fixture.AddInstance();
        _fixture.FileSystem.AddFile("/fleet/copy/file.txt", new MockFileData("data"));

        OperationResult<DuplicateOutcome> result = await CreateHandler()
            .DuplicateAsync(LifecycleFixture.Root, new DuplicateRequest("demo", "copy"));

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
        _fixture.Wsl.ImportedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Rolls_back_the_target_and_restarts_the_source_when_import_fails()
    {
        _fixture.AddInstance(running: true);
        _fixture.Wsl.ImportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--import"]),
            new ProcessResult(1, string.Empty, "import failed"));

        OperationResult<DuplicateOutcome> result = await CreateHandler()
            .DuplicateAsync(LifecycleFixture.Root, new DuplicateRequest("demo", "copy"));

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.Wsl.UnregisteredNames.Should().Contain("copy");
        _fixture.FileSystem.Directory.Exists("/fleet/copy").Should().BeFalse();
        _fixture.Wsl.Starts.Should().Equal("demo");
    }

    [Fact]
    public async Task Rejects_a_duplicate_without_enough_space()
    {
        _fixture.AddInstance();
        _fixture.DiskSpace.AvailableBytes = 15;

        OperationResult<DuplicateOutcome> result = await CreateHandler()
            .DuplicateAsync(LifecycleFixture.Root, new DuplicateRequest("demo", "copy"));

        result.Error!.Code.Should().Be(ErrorCode.DiskSpace);
        _fixture.Wsl.Exports.Should().BeEmpty();
    }
}
