using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;

namespace wsl_squad.Tests.Core.Fleet;

public class FleetMarkerStoreTests
{
    private const string _instanceDirectory = "/fleet/demo";

    [Fact]
    public void Exists_is_false_until_a_marker_is_written()
    {
        MockFileSystem fileSystem = new();
        FleetMarkerStore store = new(fileSystem);

        store.Exists(_instanceDirectory).Should().BeFalse();

        store.Write(_instanceDirectory, new FleetMarker
        {
            Id = "id-1",
            Name = "demo",
            CreatedUtc = DateTimeOffset.UnixEpoch,
            CreatedBy = "build",
        }).IsSuccess.Should().BeTrue();

        store.Exists(_instanceDirectory).Should().BeTrue();
        fileSystem.File.Exists("/fleet/demo/.wsl-squad").Should().BeTrue();
    }

    [Fact]
    public void Round_trips_marker_values()
    {
        FleetMarkerStore store = new(new MockFileSystem());
        DateTimeOffset created = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        store.Write(_instanceDirectory, new FleetMarker
        {
            Id = "id-1",
            Name = "demo",
            CreatedUtc = created,
            CreatedBy = "build",
        });

        OperationResult<FleetMarker> marker = store.Read(_instanceDirectory);
        marker.IsSuccess.Should().BeTrue();
        marker.Value.Id.Should().Be("id-1");
        marker.Value.Name.Should().Be("demo");
        marker.Value.CreatedUtc.Should().Be(created);
        marker.Value.Kind.Should().Be(FleetMarker.InstanceKind);
    }

    [Fact]
    public void Read_reports_a_missing_marker()
    {
        OperationResult<FleetMarker> result = new FleetMarkerStore(new MockFileSystem()).Read(_instanceDirectory);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Read_reports_corrupt_json()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile("/fleet/demo/.wsl-squad", new MockFileData("{broken"));

        OperationResult<FleetMarker> result = new FleetMarkerStore(fileSystem).Read(_instanceDirectory);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }
}
