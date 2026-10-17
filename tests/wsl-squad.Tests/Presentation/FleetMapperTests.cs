using FluentAssertions;

using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Mappers;

namespace wsl_squad.Tests.Presentation;

public class FleetMapperTests
{
    [Fact]
    public void Maps_a_snapshot_to_display_rows()
    {
        FleetSnapshot snapshot = new(
            "/fleet",
            [new WslInstanceInfo("alpha", "/fleet/alpha", true, "yes", 1536, null)],
            [new WslArchiveInfo("old", "/fleet/archives/old", "old.tar.gz", "tar.gz", DateTimeOffset.UnixEpoch, 2048, new ArchiveMetadata())],
            [new WslOrphanInfo("ghost", "/fleet/ghost", 0)],
            ["warning"]);

        FleetSnapshotDto dto = snapshot.ToDto();

        dto.Root.Should().Be("/fleet");
        dto.Instances.Should().ContainSingle();
        dto.Instances[0].Name.Should().Be("alpha");
        dto.Instances[0].Size.Should().Be("1.5 KB");
        dto.Instances[0].IsRunning.Should().BeTrue();
        dto.Archives[0].Size.Should().Be("2.0 KB");
        dto.Orphans[0].Size.Should().Be("0 B");
        dto.Warnings.Should().Equal("warning");
    }
}
