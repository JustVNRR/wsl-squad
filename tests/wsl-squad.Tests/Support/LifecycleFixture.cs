using System.IO.Abstractions.TestingHelpers;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Tests.Support;

public sealed class LifecycleFixture
{
    public const string Root = "/fleet";

    public MockFileSystem FileSystem { get; } = new();

    public FakeWslGateway Wsl { get; } = new();

    public FakeWslRegistryGateway Registry { get; } = new();

    public FakeDiskSpaceProbe DiskSpace { get; } = new();

    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    public FleetMarkerStore Markers { get; }

    public InstanceMetadataStore Metadata { get; }

    public ArchiveStore Archives { get; }

    public InstanceStateService Instances { get; }

    public LifecycleFixture()
    {
        Markers = new FleetMarkerStore(FileSystem);
        Metadata = new InstanceMetadataStore(FileSystem, Clock);
        Archives = new ArchiveStore(FileSystem);
        Instances = new InstanceStateService(FileSystem, Markers, Wsl);
    }

    public void AddInstance(
        string name = "demo",
        string virtualDisk = "0123456789",
        bool running = false,
        string defaultUser = "root",
        string docker = "unknown")
    {
        string directory = $"{Root}/{name}";
        FileSystem.AddFile($"{directory}/.wsl-squad", new MockFileData("{}"));
        FileSystem.AddFile(
            $"{directory}/instance.json",
            new MockFileData(
                $$"""{"schemaVersion":1,"name":"{{name}}","defaultUser":"{{defaultUser}}","dockerDesktop":"{{docker}}"}"""));
        FileSystem.AddFile($"{directory}/ext4.vhdx", new MockFileData(virtualDisk));

        if (running)
        {
            Wsl.SetRunning(name);
        }
    }

    public void ExportWritesArchive()
    {
        Wsl.ExportAction = (_, path, _) => FileSystem.File.WriteAllText(path, "archive-data");
    }

    public string AddArchive(string name = "old", string content = "tar-data", string? sha256 = null)
    {
        string directory = $"{Root}/archives/{name}";
        string file = $"{name}.tar.gz";
        FileSystem.AddFile($"{directory}/{file}", new MockFileData(content));
        string hash = sha256 ?? FileHash.Sha256(FileSystem, $"{directory}/{file}");

        FileSystem.AddFile(
            $"{directory}/archive.json",
            new MockFileData(
                $$"""{"schemaVersion":1,"name":"{{name}}","sourceInstance":"demo","createdUtc":"2026-10-17T12:00:00+00:00","format":"tar.gz","file":"{{file}}","sha256":"{{hash}}","defaultUser":"jane","dockerDesktop":"yes"}"""));

        return directory;
    }

    public ArchiveService CreateArchiveService()
    {
        return new ArchiveService(FileSystem, Instances, Wsl, Archives, Metadata, DiskSpace, Clock);
    }
}
