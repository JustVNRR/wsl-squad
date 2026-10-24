using WslSquad.Core.Dtos;

namespace WslSquad.Core.Services;

public interface IPackCatalogService
{
    IReadOnlyList<WslPackDto> GetAllPacks();

    string CreatePack(CreatePackDto dto);

    void DeletePack(string packName);

    void OpenPackFolder(string packName);
}
