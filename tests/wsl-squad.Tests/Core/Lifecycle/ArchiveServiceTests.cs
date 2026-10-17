using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class ArchiveServiceTests
{
    private readonly LifecycleFixture _fixture = new();

    private static ArchiveRequest Request(
        bool replace = false,
        bool stopIfRunning = false,
        string? archiveName = null)
    {
        return new ArchiveRequest("demo", archiveName, "tar.gz", replace, stopIfRunning);
    }

    [Fact]
    public async Task Archives_an_instance_with_checksum_and_metadata()
    {
        _fixture.AddInstance(defaultUser: "jane", docker: "yes");
        _fixture.ExportWritesArchive();

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.ArchiveName.Should().Be("demo_archive");
        result.Value.SizeBytes.Should().Be(12);
        result.Value.Restarted.Should().BeFalse();
        _fixture.FileSystem.File.Exists("/fleet/archives/demo_archive/demo_archive.tar.gz").Should().BeTrue();
        _fixture.Wsl.Exports.Should().ContainSingle().Which.Should().Contain("demo_archive.tar.gz");

        OperationResult<ArchiveMetadata> metadata = _fixture.Archives.Read("/fleet/archives/demo_archive");
        metadata.Value.SourceInstance.Should().Be("demo");
        metadata.Value.DefaultUser.Should().Be("jane");
        metadata.Value.DockerDesktop.Should().Be("yes");
        metadata.Value.Sha256.Should().HaveLength(64);
    }

    [Fact]
    public async Task Refuses_to_replace_an_archive_without_permission()
    {
        _fixture.AddInstance();
        _fixture.AddArchive("demo_archive");

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
        _fixture.Wsl.Exports.Should().BeEmpty();
    }

    [Fact]
    public async Task Replaces_an_existing_archive_when_allowed()
    {
        _fixture.AddInstance();
        _fixture.AddArchive("demo_archive", content: "old-data");
        _fixture.ExportWritesArchive();

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request(replace: true));

        result.IsSuccess.Should().BeTrue();
        string content = await _fixture.FileSystem.File.ReadAllTextAsync("/fleet/archives/demo_archive/demo_archive.tar.gz");
        content.Should().Be("archive-data");
    }

    [Fact]
    public async Task Requires_permission_to_stop_a_running_instance()
    {
        _fixture.AddInstance(running: true);

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.ConfirmationRequired);
        _fixture.Wsl.Exports.Should().BeEmpty();
        _fixture.Wsl.TerminatedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Stops_and_restarts_a_running_instance()
    {
        _fixture.AddInstance(running: true);
        _fixture.ExportWritesArchive();

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request(stopIfRunning: true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Restarted.Should().BeTrue();
        _fixture.Wsl.TerminatedNames.Should().Equal("demo");
        _fixture.Wsl.Starts.Should().Equal("demo");
    }

    [Fact]
    public async Task Removes_partial_archives_and_restarts_after_an_export_failure()
    {
        _fixture.AddInstance(running: true);
        _fixture.Wsl.ExportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--export"]),
            new ProcessResult(1, string.Empty, "export failed"));

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request(stopIfRunning: true));

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.FileSystem.Directory.Exists("/fleet/archives/demo_archive").Should().BeFalse();
        _fixture.Wsl.Starts.Should().Equal("demo");
    }

    [Fact]
    public async Task Fails_before_export_when_space_is_insufficient()
    {
        _fixture.AddInstance();
        _fixture.DiskSpace.AvailableBytes = 5;

        OperationResult<ArchiveOutcome> result = await _fixture.CreateArchiveService()
            .ArchiveAsync(LifecycleFixture.Root, Request());

        result.Error!.Code.Should().Be(ErrorCode.DiskSpace);
        _fixture.Wsl.Exports.Should().BeEmpty();
    }
}
