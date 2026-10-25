using WslSquad.Core.Repositories;
using WslSquad.Core.Services;
using WslSquad.Presentation.Localization;

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
        Localizer.Instance.LoadLanguages(Path.Combine(assets, "languages"));
        Localizer.Instance.UsePreferences(new ConfigRepository(assets));

        ServiceFactory factory = new();
        IFleetService fleet = factory.CreateFleetService(assets);
        fleet.GetRoot();

        return new CliServices(
            factory.CreateInstanceService(assets),
            fleet,
            factory.CreatePackCatalogService(assets));
    }
}
