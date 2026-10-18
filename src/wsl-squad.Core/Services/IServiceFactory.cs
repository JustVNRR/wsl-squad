using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Services;

public interface IServiceFactory
{
    public IInstanceService CreateInstanceService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null);

    public ILoggerService CreateLoggerService();

    public string ResolveRoot(string assets, string distroTree);

    public IFleetService CreateFleetService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null);

    public IPackCatalogService CreatePackCatalogService(string assets, string distroTree);
}
