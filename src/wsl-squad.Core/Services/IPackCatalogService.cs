using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface IPackCatalogService
{
    IReadOnlyList<WslPackDto> GetAllPacks();

    string CreatePack(CreatePackDto dto);

    void DeletePack(string packName);

    void OpenPackFolder(string packName);
}
