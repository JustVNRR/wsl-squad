namespace WslSquad.Core.Services;

public interface IServiceFactory
{
    IInstanceService CreateInstanceService(string assets);

    ILoggerService CreateLoggerService();

    IFleetService CreateFleetService(string assets);

    IPackCatalogService CreatePackCatalogService(string assets);
}
