using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

/// <summary>The pack catalogue's editing operations.</summary>
public interface IPackCatalogService
{
    public IReadOnlyList<WslPackDto> GetAllPacks();

    public WslPackDto? GetPack(string packName);

    public string CreatePack(CreatePackDto dto);

    public void SetPackDescription(SetPackDescriptionDto dto);

    public void DeletePack(string packName);

    public void OpenPackFolder(string packName);
}
