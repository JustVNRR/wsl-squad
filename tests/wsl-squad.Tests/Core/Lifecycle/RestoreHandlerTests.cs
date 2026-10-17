using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class RestoreHandlerTests
{
    private readonly LifecycleFixture _fixture = new();

    private RestoreHandler CreateHandler()
    {
        return new RestoreHandler(
            _fixture.FileSystem,
            _fixture.Wsl,
            _fixture.Registry,
            _fixture.Archives,
            _fixture.Markers,
            _fixture.Metadata,
            _fixture.DiskSpace,
            _fixture.Clock);
    }

    private static RestoreRequest Request(string archive = "old", string instance = "restored")
    {
        return new RestoreRequest(archive, instance);
    }

    [Fact]
    public async Task Restores_an_archive_with_metadata_and_icon()
    {
        _fixture.AddArchive("old");
        _fixture.FileSystem.AddFile("/fleet/archives/old/terminal-icon.png", new MockFileData("png"));

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultUser.Should().Be("jane");
        _fixture.Wsl.ImportedNames.Should().Equal("restored");
        _fixture.FileSystem.File.Exists("/fleet/restored/.wsl-squad").Should().BeTrue();
        _fixture.FileSystem.File.Exists("/fleet/restored/terminal-icon.png").Should().BeTrue();

        OperationResult<InstanceMetadata> metadata = _fixture.Metadata.Read("/fleet/restored");
        metadata.Value.DefaultUser.Should().Be("jane");
        metadata.Value.DockerDesktop.Should().Be("yes");
        metadata.Value.Source!.Kind.Should().Be("Restore");
        metadata.Value.Source.Reference.Should().Be("old");
    }

    [Fact]
    public async Task Rejects_an_archive_with_a_bad_checksum()
    {
        _fixture.AddArchive("old", sha256: new string('0', 64));

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
        _fixture.Wsl.ImportedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_a_missing_archive_file()
    {
        _fixture.AddArchive("old");
        _fixture.FileSystem.File.Delete("/fleet/archives/old/old.tar.gz");

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Rolls_back_when_import_fails()
    {
        _fixture.AddArchive("old");
        _fixture.Wsl.ImportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--import"]),
            new ProcessResult(1, string.Empty, "import failed"));

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.Wsl.UnregisteredNames.Should().Contain("restored");
        _fixture.FileSystem.Directory.Exists("/fleet/restored").Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_an_existing_instance_folder()
    {
        _fixture.AddArchive("old");
        _fixture.FileSystem.AddFile("/fleet/restored/file.txt", new MockFileData("data"));

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public async Task Rejects_a_registered_instance_name()
    {
        _fixture.AddArchive("old");
        _fixture.Registry.Add("restored", "/fleet/restored");

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public async Task Rejects_a_restore_without_enough_space()
    {
        _fixture.AddArchive("old");
        _fixture.DiskSpace.AvailableBytes = 5;

        OperationResult<RestoreOutcome> result = await CreateHandler()
            .RestoreAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.DiskSpace);
        _fixture.Wsl.ImportedNames.Should().BeEmpty();
    }
}
