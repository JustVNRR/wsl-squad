namespace WslSquad.Core.Services;

public interface IServiceFactory
{
    IInstanceService CreateInstanceService(
        string root,
        string assets,
        string distroTree);

    ILoggerService CreateLoggerService();

    string ResolveRoot(string assets, string distroTree);

    IFleetService CreateFleetService(
        string root,
        string assets,
        string distroTree);

    IPackCatalogService CreatePackCatalogService(string assets, string distroTree);
}
