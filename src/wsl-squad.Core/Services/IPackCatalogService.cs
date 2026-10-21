using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface IPackCatalogService
{
    public IReadOnlyList<WslPackDto> GetAllPacks();

    public string CreatePack(CreatePackDto dto);

    public void DeletePack(string packName);

    public void OpenPackFolder(string packName);
}
