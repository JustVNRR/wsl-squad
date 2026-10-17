using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;

namespace wsl_squad.Tests.Core.Fleet;

public class FleetConfigStoreTests
{
    private const string _configPath = "/config/config.json";

    [Fact]
    public void Load_returns_null_when_the_file_is_missing()
    {
        FleetConfigStore store = new(new MockFileSystem(), _configPath);

        OperationResult<FleetConfig?> result = store.Load();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Save_then_load_round_trips_the_fleet_root()
    {
        MockFileSystem fileSystem = new();
        FleetConfigStore store = new(fileSystem, _configPath);

        store.Save(new FleetConfig { FleetRoot = "/fleet" }).IsSuccess.Should().BeTrue();

        OperationResult<FleetConfig?> loaded = store.Load();
        loaded.Value!.FleetRoot.Should().Be("/fleet");
        fileSystem.File.Exists(_configPath).Should().BeTrue();
        fileSystem.File.ReadAllText(_configPath).Should().Contain("\"fleetRoot\"");
    }

    [Fact]
    public void Save_leaves_no_temporary_file_behind()
    {
        MockFileSystem fileSystem = new();
        FleetConfigStore store = new(fileSystem, _configPath);

        store.Save(new FleetConfig { FleetRoot = "/fleet" });

        fileSystem.Directory.GetFiles("/config").Should().ContainSingle().Which.Should().Be(_configPath);
    }

    [Fact]
    public void Load_reports_corrupt_json()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile(_configPath, new MockFileData("{not json"));
        FleetConfigStore store = new(fileSystem, _configPath);

        OperationResult<FleetConfig?> result = store.Load();

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }

    [Fact]
    public void Load_rejects_a_missing_fleet_root()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile(_configPath, new MockFileData("{}"));
        FleetConfigStore store = new(fileSystem, _configPath);

        OperationResult<FleetConfig?> result = store.Load();

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }
}
