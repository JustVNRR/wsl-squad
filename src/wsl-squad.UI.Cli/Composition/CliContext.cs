using System.IO.Abstractions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Environment;
using wsl_squad.Core.Infrastructure.Process;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Core.Infrastructure.Time;
using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.UI.Cli.Composition;

/// <summary>Object graph shared by lifecycle commands.</summary>
internal sealed class CliContext
{
    private readonly FleetRootResolver _rootResolver;

    private CliContext()
    {
        FileSystem = new FileSystem();
        Platform = new SystemPlatformEnvironment();
        Clock = new SystemClock();
        ConfigStore = new FleetConfigStore(FileSystem, AppPaths.GetConfigFilePath(Platform));
        Markers = new FleetMarkerStore(FileSystem);
        Registry = new WindowsRegistryGateway();
        ProcessRunner processRunner = new();
        ProcessRunner = processRunner;
        Wsl = new WslCliGateway(processRunner);
        Archives = new ArchiveStore(FileSystem);
        Metadata = new InstanceMetadataStore(FileSystem, Clock);
        DiskSpace = new DriveInfoDiskSpaceProbe();
        Instances = new InstanceStateService(FileSystem, Markers, Wsl);
        Terminal = new TerminalLauncher(new ProcessLauncher());
        ArchiveService = new ArchiveService(FileSystem, Instances, Wsl, Archives, Metadata, DiskSpace, Clock);
        _rootResolver = new FleetRootResolver(Platform, ConfigStore, Registry, Markers);
    }

    public FileSystem FileSystem { get; }

    public SystemPlatformEnvironment Platform { get; }

    public SystemClock Clock { get; }

    public FleetConfigStore ConfigStore { get; }

    public FleetMarkerStore Markers { get; }

    public WindowsRegistryGateway Registry { get; }

    public ProcessRunner ProcessRunner { get; }

    public WslCliGateway Wsl { get; }

    public ArchiveStore Archives { get; }

    public InstanceMetadataStore Metadata { get; }

    public DriveInfoDiskSpaceProbe DiskSpace { get; }

    public InstanceStateService Instances { get; }

    public TerminalLauncher Terminal { get; }

    public ArchiveService ArchiveService { get; }

    public static CliContext Create()
    {
        return new CliContext();
    }

    public OperationResult<string> ResolveRoot()
    {
        return _rootResolver.Resolve();
    }
}
