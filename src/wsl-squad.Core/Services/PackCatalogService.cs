using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;
using wsl_squad.Core.Mappers;
using wsl_squad.Core.Repositories;

namespace wsl_squad.Core.Services;

public sealed class PackCatalogService : IPackCatalogService
{
    private readonly WslPackCatalog _catalog;
    private readonly IPackCatalogRepository _packCatalogRepository;
    private readonly IInstanceMapper _instanceMapper;

    public PackCatalogService(
        WslPackCatalog catalog,
        IPackCatalogRepository packCatalogRepository,
        IInstanceMapper instanceMapper)
    {
        _catalog = catalog;
        _packCatalogRepository = packCatalogRepository;
        _instanceMapper = instanceMapper;
    }

    public IReadOnlyList<WslPackDto> GetAllPacks()
    {
        RefreshCatalog();
        List<WslPackDto> rows = [];

        foreach (WslPack pack in _catalog.Packs)
        {
            rows.Add(_instanceMapper.ToPackDto(pack) with
            {
                Dependents = [.. _catalog.DependentsOf(pack.Name)],
            });
        }

        return rows;
    }

    public string CreatePack(CreatePackDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string newName = dto.NewName ?? string.Empty;
        _ = new WslPack { Name = newName };

        _packCatalogRepository.CopyPack(dto.SourceName ?? string.Empty, newName);

        if (!string.IsNullOrEmpty(dto.Description))
        {
            _packCatalogRepository.SavePackDescription(newName, dto.Description);
        }

        return newName;
    }

    public void DeletePack(string packName)
    {
        RequirePack(packName);
        _packCatalogRepository.DeletePack(packName);
    }

    public void OpenPackFolder(string packName)
    {
        RequirePack(packName);
        _packCatalogRepository.OpenFolder(packName);
    }

    private void RefreshCatalog()
    {
        _catalog.Packs = _packCatalogRepository.GetAllPacks();
    }

    private void RequirePack(string packName)
    {
        RefreshCatalog();

        if (_catalog.GetPack(packName) is null)
        {
            throw new InvalidOperationException($"There is no pack named '{packName}' in the catalogue.");
        }
    }
}
