using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Presenters.Fleet;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Presentation;

public class FleetListPresenterTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Loads_a_snapshot_for_the_resolved_root()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile("/fleet/alpha/.wsl-squad", new MockFileData("{}"));
        fileSystem.AddFile(
            "/fleet/alpha/instance.json",
            new MockFileData("""{"schemaVersion":1,"name":"alpha","dockerDesktop":"yes"}"""));
        fileSystem.AddFile("/fleet/alpha/ext4.vhdx", new MockFileData("0123456789"));

        FakePlatformEnvironment platform = new();
        platform.SetVariable(AppPaths.FleetRootVariable, "/fleet");
        FakeWslRegistryGateway registry = new();
        registry.Add("alpha", "/fleet/alpha");
        FakeWslGateway wsl = new();
        wsl.SetRunning("alpha");

        FleetListPresenter presenter = new(
            new FleetRootResolver(
                platform,
                new FleetConfigStore(fileSystem, "/config/config.json"),
                registry,
                new FleetMarkerStore(fileSystem)),
            new FleetDiscoveryService(
                fileSystem,
                registry,
                wsl,
                new FleetMarkerStore(fileSystem),
                new InstanceMetadataStore(fileSystem, new FakeClock(_now)),
                new ArchiveStore(fileSystem)));

        OperationResult<FleetSnapshotDto> result = await presenter.LoadAsync(includeArchives: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Root.Should().Be("/fleet");
        result.Value.Instances.Should().ContainSingle();
        result.Value.Instances[0].Name.Should().Be("alpha");
        result.Value.Instances[0].IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task Propagates_discovery_failures()
    {
        MockFileSystem fileSystem = new();
        FakePlatformEnvironment platform = new();
        platform.SetVariable(AppPaths.FleetRootVariable, "/fleet");
        FakeWslRegistryGateway registry = new() { IsSupported = false };

        FleetListPresenter presenter = new(
            new FleetRootResolver(
                platform,
                new FleetConfigStore(fileSystem, "/config/config.json"),
                registry,
                new FleetMarkerStore(fileSystem)),
            new FleetDiscoveryService(
                fileSystem,
                registry,
                new FakeWslGateway(),
                new FleetMarkerStore(fileSystem),
                new InstanceMetadataStore(fileSystem, new FakeClock(_now)),
                new ArchiveStore(fileSystem)));

        OperationResult<FleetSnapshotDto> result = await presenter.LoadAsync(includeArchives: false);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.TerminalUnavailable);
    }
}
