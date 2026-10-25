using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;
using WslSquad.Core.Repositories;

namespace WslSquad.Core.Services;

public sealed class ServiceFactory : IServiceFactory
{
    public IInstanceService CreateInstanceService(string assets)
    {
        ConfigRepository config = new(assets, assets);

        return new InstanceService(
            new InstanceRepository(config),
            new ArchiveRepository(config),
            new WslRepository(),
            new TerminalRepository(assets),
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

    public IFleetService CreateFleetService(string assets)
    {
        ConfigRepository config = new(assets, assets);
        InstanceRepository instances = new(config);
        ArchiveRepository archives = new(config);
        WslRepository wsl = new();
        InstanceService service = new(
            instances,
            archives,
            wsl,
            new TerminalRepository(assets),
            new DockerRepository(assets),
            new PackCatalogRepository(Path.Combine(assets, "packs")),
            config,
            new InstanceMapper());

        return new FleetService(config, wsl, instances, archives, service);
    }

    public IPackCatalogService CreatePackCatalogService(string assets)
    {
        return new PackCatalogService(
            new WslPackCatalog(),
            new PackCatalogRepository(Path.Combine(assets, "packs")),
            new InstanceMapper());
    }
}
