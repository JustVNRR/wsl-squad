using WslSquad.Core.Services;

namespace WslSquad.UI.Cli.Composition;

public sealed class CliServices
{
    private CliServices(IInstanceService instance, IFleetService fleet, IPackCatalogService catalogue)
    {
        Instance = instance;
        Fleet = fleet;
        Catalogue = catalogue;
    }

    public IInstanceService Instance { get; }

    public IFleetService Fleet { get; }

    public IPackCatalogService Catalogue { get; }

    public static CliServices Create()
    {
        string assets = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wsl-squad");
        Directory.CreateDirectory(assets);
        CatalogBootstrap.Ensure(assets);

        ServiceFactory factory = new();
        string root = factory.ResolveRoot(assets, assets);

        return new CliServices(
            factory.CreateInstanceService(root, assets, assets),
            factory.CreateFleetService(root, assets, assets),
            factory.CreatePackCatalogService(assets, assets));
    }
}
