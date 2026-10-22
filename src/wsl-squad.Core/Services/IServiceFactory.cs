using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Services;

public interface IServiceFactory
{
    IInstanceService CreateInstanceService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null);

    ILoggerService CreateLoggerService();

    string ResolveRoot(string assets, string distroTree);

    IFleetService CreateFleetService(
        string root,
        string assets,
        string distroTree,
        Func<string, string, IconDrawing?, IconDrawing>? drawIcon = null);

    IPackCatalogService CreatePackCatalogService(string assets, string distroTree);
}
