using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public interface IPackCatalogRepository
{
    public void SeedFrom(string sourceRoot);

    public string GetRoot();

    public string GetFolder(string packName);

    public IReadOnlyList<WslPack> GetAllPacks();

    public void SavePackDescription(string packName, string description);

    public void CopyPack(string sourceName, string newName);

    public void DeletePack(string packName);

    public void OpenFolder(string packName);
}
