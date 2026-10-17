using System.ComponentModel;
using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Builds a read-only view of the fleet rooted at a directory.</summary>
public sealed class FleetDiscoveryService
{
    private static readonly string[] _reservedFolders =
        [ArchiveStore.ArchivesFolderName, ".staging", ".locks"];

    private readonly IFileSystem _fileSystem;
    private readonly IWslRegistryGateway _registry;
    private readonly IWslGateway _wsl;
    private readonly FleetMarkerStore _markers;
    private readonly InstanceMetadataStore _metadataStore;
    private readonly ArchiveStore _archiveStore;

    public FleetDiscoveryService(
        IFileSystem fileSystem,
        IWslRegistryGateway registry,
        IWslGateway wsl,
        FleetMarkerStore markers,
        InstanceMetadataStore metadataStore,
        ArchiveStore archiveStore)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _wsl = wsl;
        _markers = markers;
        _metadataStore = metadataStore;
        _archiveStore = archiveStore;
    }

    public async Task<OperationResult<FleetSnapshot>> GetSnapshotAsync(
        string root,
        bool includeArchives,
        CancellationToken cancellationToken = default)
    {
        if (!_registry.IsSupported)
        {
            return OperationResult<FleetSnapshot>.Fail(
                ErrorCode.TerminalUnavailable,
                "WSL fleet discovery is only available on Windows.");
        }

        IReadOnlyList<WslRegistryEntry> registryEntries;

        try
        {
            registryEntries = _registry.GetDistributions();
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            return OperationResult<FleetSnapshot>.Fail(
                ErrorCode.TerminalUnavailable,
                "WSL distributions could not be read from the registry.",
                exception.Message);
        }

        IReadOnlyList<string> runningNames;

        try
        {
            runningNames = await _wsl.GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ProcessExecutionException exception)
        {
            return OperationResult<FleetSnapshot>.Fail(
                ErrorCode.ExternalTool,
                "wsl.exe could not list running distributions.",
                exception.Result.StandardError,
                exception.Result.ExitCode);
        }
        catch (Win32Exception exception)
        {
            return OperationResult<FleetSnapshot>.Fail(
                ErrorCode.ExternalTool,
                "wsl.exe is not available.",
                exception.Message);
        }

        HashSet<string> running = new(runningNames, StringComparer.OrdinalIgnoreCase);
        List<WslInstanceInfo> instances = [];
        List<string> warnings = [];
        HashSet<string> registeredPaths = new(StringComparer.Ordinal);

        foreach (WslRegistryEntry entry in registryEntries)
        {
            if (!PathHelper.IsFullyQualified(entry.BasePath))
            {
                continue;
            }

            string? parent = PathHelper.GetParent(entry.BasePath);
            if (!PathHelper.AreSamePath(parent, root) || !_markers.Exists(entry.BasePath))
            {
                continue;
            }

            _ = registeredPaths.Add(PathHelper.NormalizeForComparison(entry.BasePath));

            InstanceMetadata? metadata = null;
            OperationResult<InstanceMetadata> metadataResult = _metadataStore.Read(entry.BasePath);

            if (metadataResult.IsSuccess)
            {
                metadata = metadataResult.Value;
            }
            else
            {
                warnings.Add($"{entry.Name}: {metadataResult.Error!.Message}");
            }

            string virtualDiskPath = PathHelper.NormalizeSeparators(entry.BasePath) + "/ext4.vhdx";

            instances.Add(new WslInstanceInfo(
                entry.Name,
                entry.BasePath,
                running.Contains(entry.Name),
                metadata?.DockerDesktop ?? InstanceMetadata.DockerUnknown,
                GetFileLength(virtualDiskPath),
                metadata));
        }

        instances.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        List<WslOrphanInfo> orphans = FindOrphans(root, registeredPaths);

        List<WslArchiveInfo> archives = [];
        if (includeArchives)
        {
            OperationResult<ArchiveListing> listing = _archiveStore.List(root);

            if (listing.IsSuccess)
            {
                archives.AddRange(listing.Value.Archives);
                warnings.AddRange(listing.Value.Warnings);
            }
            else
            {
                warnings.Add(listing.Error!.Message);
            }
        }

        return OperationResult<FleetSnapshot>.Ok(
            new FleetSnapshot(root, instances, archives, orphans, warnings));
    }

    private List<WslOrphanInfo> FindOrphans(string root, HashSet<string> registeredPaths)
    {
        List<WslOrphanInfo> orphans = [];

        if (!_fileSystem.Directory.Exists(root))
        {
            return orphans;
        }

        try
        {
            foreach (string directory in _fileSystem.Directory.GetDirectories(root))
            {
                string name = _fileSystem.Path.GetFileName(directory);

                if (_reservedFolders.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    !_markers.Exists(directory) ||
                    registeredPaths.Contains(PathHelper.NormalizeForComparison(directory)))
                {
                    continue;
                }

                orphans.Add(new WslOrphanInfo(
                    name,
                    directory,
                    GetFileLength(_fileSystem.Path.Combine(directory, "ext4.vhdx"))));
            }
        }
        catch (IOException)
        {
            return orphans;
        }
        catch (UnauthorizedAccessException)
        {
            return orphans;
        }

        orphans.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        return orphans;
    }

    private long GetFileLength(string path)
    {
        try
        {
            return _fileSystem.File.Exists(path) ? _fileSystem.FileInfo.New(path).Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
