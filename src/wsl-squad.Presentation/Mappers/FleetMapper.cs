using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Formatting;

namespace wsl_squad.Presentation.Mappers;

/// <summary>Maps fleet domain records to presentation DTOs.</summary>
public static class FleetMapper
{
    public static FleetSnapshotDto ToDto(this FleetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new FleetSnapshotDto(
            snapshot.Root,
            [.. snapshot.Instances.Select(static instance => instance.ToDto())],
            [.. snapshot.Archives.Select(static archive => archive.ToDto())],
            [.. snapshot.Orphans.Select(static orphan => orphan.ToDto())],
            [.. snapshot.Warnings]);
    }

    public static InstanceRowDto ToDto(this WslInstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new InstanceRowDto(
            instance.Name,
            instance.Path,
            instance.IsRunning,
            instance.DockerDesktop,
            instance.SizeBytes,
            SizeFormatter.Format(instance.SizeBytes));
    }

    public static ArchiveRowDto ToDto(this WslArchiveInfo archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        return new ArchiveRowDto(
            archive.Name,
            archive.Path,
            archive.FileName,
            archive.Format,
            archive.SizeBytes,
            SizeFormatter.Format(archive.SizeBytes),
            archive.ArchivedAt);
    }

    public static OrphanRowDto ToDto(this WslOrphanInfo orphan)
    {
        ArgumentNullException.ThrowIfNull(orphan);

        return new OrphanRowDto(
            orphan.Name,
            orphan.Path,
            orphan.SizeBytes,
            SizeFormatter.Format(orphan.SizeBytes));
    }
}
