using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Fleet;

public class FleetDiscoveryServiceTests
{
    private const string _root = "/fleet";
    private static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeWslRegistryGateway _registry = new();
    private readonly FakeWslGateway _wsl = new();

    private FleetDiscoveryService CreateService()
    {
        return new FleetDiscoveryService(
            _fileSystem,
            _registry,
            _wsl,
            new FleetMarkerStore(_fileSystem),
            new InstanceMetadataStore(_fileSystem, new FakeClock(_now)),
            new ArchiveStore(_fileSystem));
    }

    private void AddInstance(string name, bool withMetadata = true, string docker = "unknown", string virtualDisk = "0123456789")
    {
        string directory = $"{_root}/{name}";
        _fileSystem.AddFile($"{directory}/.wsl-squad", new MockFileData("{}"));

        if (withMetadata)
        {
            _fileSystem.AddFile(
                $"{directory}/instance.json",
                new MockFileData($$"""{"schemaVersion":1,"name":"{{name}}","dockerDesktop":"{{docker}}"}"""));
        }

        _fileSystem.AddFile($"{directory}/ext4.vhdx", new MockFileData(virtualDisk));
        _registry.Add(name, directory);
    }

    [Fact]
    public async Task Lists_managed_instances_with_state_and_size()
    {
        AddInstance("alpha", docker: "yes");
        AddInstance("beta", docker: "no");
        _wsl.SetRunning("alpha");

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Instances.Should().HaveCount(2);
        result.Value.Instances[0].Name.Should().Be("alpha");
        result.Value.Instances[0].IsRunning.Should().BeTrue();
        result.Value.Instances[0].DockerDesktop.Should().Be("yes");
        result.Value.Instances[0].SizeBytes.Should().Be(10);
        result.Value.Instances[1].Name.Should().Be("beta");
        result.Value.Instances[1].IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Ignores_registrations_outside_the_root()
    {
        _registry.Add("outer", "/elsewhere/outer");
        _fileSystem.AddFile("/elsewhere/outer/.wsl-squad", new MockFileData("{}"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Instances.Should().BeEmpty();
        result.Value.Orphans.Should().BeEmpty();
    }

    [Fact]
    public async Task Ignores_unmarked_folders()
    {
        _registry.Add("nomarker", "/fleet/nomarker");
        _fileSystem.AddFile("/fleet/nomarker/ext4.vhdx", new MockFileData("data"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Instances.Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_orphans_without_deleting_them()
    {
        _fileSystem.AddFile("/fleet/orphan/.wsl-squad", new MockFileData("{}"));
        _fileSystem.AddFile("/fleet/orphan/ext4.vhdx", new MockFileData("0123456789"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Orphans.Should().ContainSingle();
        result.Value.Orphans[0].Name.Should().Be("orphan");
        result.Value.Orphans[0].SizeBytes.Should().Be(10);
        _fileSystem.Directory.Exists("/fleet/orphan").Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_treat_reserved_folders_as_orphans()
    {
        _fileSystem.AddFile("/fleet/archives/.wsl-squad", new MockFileData("{}"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Orphans.Should().BeEmpty();
    }

    [Fact]
    public async Task Warns_about_missing_metadata_but_still_lists_the_instance()
    {
        AddInstance("alpha", withMetadata: false);

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Instances.Should().ContainSingle();
        result.Value.Warnings.Should().Contain(message => message.Contains("alpha", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Warns_about_corrupt_metadata()
    {
        AddInstance("alpha");
        _fileSystem.AddFile("/fleet/alpha/instance.json", new MockFileData("{broken"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Value.Instances.Should().ContainSingle();
        result.Value.Warnings.Should().Contain(message => message.Contains("not valid JSON", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Lists_archives_only_when_requested()
    {
        _fileSystem.AddFile(
            "/fleet/archives/old/archive.json",
            new MockFileData(
                """{"schemaVersion":1,"name":"old","sourceInstance":"alpha","createdUtc":"2026-10-17T12:00:00+00:00","format":"tar.gz","file":"old.tar.gz"}"""));
        _fileSystem.AddFile("/fleet/archives/old/old.tar.gz", new MockFileData("data"));

        FleetDiscoveryService service = CreateService();

        OperationResult<FleetSnapshot> withoutArchives = await service.GetSnapshotAsync(_root, includeArchives: false);
        OperationResult<FleetSnapshot> withArchives = await service.GetSnapshotAsync(_root, includeArchives: true);

        withoutArchives.Value.Archives.Should().BeEmpty();
        withArchives.Value.Archives.Should().ContainSingle();
        withArchives.Value.Archives[0].Name.Should().Be("old");
        withArchives.Value.Archives[0].Format.Should().Be("tar.gz");
    }

    [Fact]
    public async Task Fails_when_the_registry_is_unsupported()
    {
        _registry.IsSupported = false;

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.TerminalUnavailable);
    }

    [Fact]
    public async Task Fails_when_wsl_cannot_list_running_distributions()
    {
        _wsl.Failure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--list", "--running", "--quiet"]),
            new ProcessResult(1, string.Empty, "failure"));

        OperationResult<FleetSnapshot> result = await CreateService().GetSnapshotAsync(_root, includeArchives: false);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        result.Error.ExternalExitCode.Should().Be(1);
    }
}
