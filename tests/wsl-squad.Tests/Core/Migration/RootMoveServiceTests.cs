using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Application.Migration;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Migration;

public class RootMoveServiceTests
{
    private const string _target = "/newfleet";

    private readonly LifecycleFixture _fixture = new();
    private readonly FleetConfigStore _config;

    public RootMoveServiceTests()
    {
        _config = new FleetConfigStore(_fixture.FileSystem, "/config/config.json");
    }

    private RootMoveService CreateService()
    {
        ArchiveService archive = _fixture.CreateArchiveService();
        RemoveInstanceHandler remove = new(
            _fixture.FileSystem,
            _fixture.Instances,
            _fixture.Wsl,
            _fixture.Registry,
            archive);

        return new RootMoveService(
            _fixture.FileSystem,
            _fixture.Markers,
            archive,
            remove,
            _fixture.Archives,
            _config);
    }

    [Fact]
    public async Task Moves_instances_archives_and_configuration()
    {
        _fixture.AddInstance("alpha");
        _fixture.AddInstance("beta");
        _fixture.Registry.Add("alpha", "/fleet/alpha");
        _fixture.Registry.Add("beta", "/fleet/beta");
        _fixture.ExportWritesArchive();

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(LifecycleFixture.Root, _target);

        result.IsSuccess.Should().BeTrue();
        result.Value.MovedInstances.Should().Equal("alpha", "beta");
        _fixture.Wsl.UnregisteredNames.Should().BeEquivalentTo("alpha", "beta");
        _fixture.FileSystem.Directory.Exists("/fleet/alpha").Should().BeFalse();
        _fixture.FileSystem.Directory.Exists("/fleet/beta").Should().BeFalse();
        _fixture.FileSystem.File.Exists("/newfleet/archives/alpha_archive/archive.json").Should().BeTrue();
        _fixture.FileSystem.File.Exists("/newfleet/archives/beta_archive/archive.json").Should().BeTrue();
        string config = await _fixture.FileSystem.File.ReadAllTextAsync("/config/config.json");
        config.Should().Contain(_target);
    }

    [Fact]
    public async Task Rejects_a_non_empty_target()
    {
        _fixture.AddInstance();
        _fixture.FileSystem.AddFile("/newfleet/existing.txt", new MockFileData("data"));

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(LifecycleFixture.Root, _target);

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
        _fixture.Wsl.Exports.Should().BeEmpty();
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeTrue();
    }

    [Fact]
    public async Task Rejects_a_target_inside_the_source()
    {
        _fixture.FileSystem.AddFile("/fleet/.keep", new MockFileData(string.Empty));

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(
            LifecycleFixture.Root,
            "/fleet/sub");

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public async Task Keeps_instances_when_archiving_fails()
    {
        _fixture.AddInstance();
        _fixture.Registry.Add("demo", "/fleet/demo");
        _fixture.Wsl.ExportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--export"]),
            new ProcessResult(1, string.Empty, "export failed"));

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(LifecycleFixture.Root, _target);

        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        _fixture.FileSystem.Directory.Exists("/fleet/demo").Should().BeTrue();
        _fixture.Wsl.UnregisteredNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Moves_an_empty_fleet()
    {
        _fixture.FileSystem.AddFile("/fleet/.keep", new MockFileData(string.Empty));

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(LifecycleFixture.Root, _target);

        result.IsSuccess.Should().BeTrue();
        result.Value.MovedInstances.Should().BeEmpty();
        string config = await _fixture.FileSystem.File.ReadAllTextAsync("/config/config.json");
        config.Should().Contain(_target);
    }

    [Fact]
    public async Task Rejects_a_target_that_already_has_archives()
    {
        _fixture.FileSystem.AddFile("/fleet/.keep", new MockFileData(string.Empty));
        _fixture.FileSystem.AddFile("/newfleet/archives/old/file.txt", new MockFileData("data"));

        OperationResult<RootMoveOutcome> result = await CreateService().MoveAsync(LifecycleFixture.Root, _target);

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }
}
