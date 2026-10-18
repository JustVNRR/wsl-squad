using wsl_squad.Core.Entities;
using wsl_squad.Core.Mappers;
using wsl_squad.Core.Repositories;

namespace wsl_squad.Core.Services;

public sealed class ServiceFactory : IServiceFactory
{
    public IInstanceService CreateInstanceService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null)
    {
        ConfigRepository config = new(assets, distroTree);

        return new InstanceService(
            new InstanceRepository(config),
            new ArchiveRepository(config),
            new WslRepository(distroTree),
            new TerminalRepository(assets, drawIcon),
            new DockerRepository(assets),
            new PackCatalogRepository(Path.Combine(assets, "packs")),
            config,
            new InstanceMapper());
    }

    public ILoggerService CreateLoggerService()
    {
        string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (baseDirectory.Length == 0)
        {
            baseDirectory = Path.GetTempPath();
        }

        return new LoggerService(Path.Combine(baseDirectory, "wsl-squad", "wsl-squad.log"));
    }

    public string ResolveRoot(string assets, string distroTree)
    {
        IFleetService fleet = CreateFleetService(Path.GetTempPath(), assets, distroTree);

        return fleet.GetRoot();
    }

    public IFleetService CreateFleetService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null)
    {
        ConfigRepository config = new(assets, distroTree);
        InstanceRepository instances = new(config);
        ArchiveRepository archives = new(config);
        WslRepository wsl = new(distroTree);
        InstanceService service = new(
            instances,
            archives,
            wsl,
            new TerminalRepository(assets, drawIcon),
            new DockerRepository(assets),
            new PackCatalogRepository(Path.Combine(assets, "packs")),
            config,
            new InstanceMapper());

        return new FleetService(config, wsl, instances, archives, service);
    }

    public IPackCatalogService CreatePackCatalogService(string assets, string distroTree)
    {
        PackCatalogRepository packs = new(Path.Combine(assets, "packs"));

        if (!string.IsNullOrEmpty(distroTree))
        {
            packs.SeedFrom(Path.Combine(distroTree, "packs"));
        }

        return new PackCatalogService(new WslPackCatalog(), packs, new InstanceMapper());
    }
}
