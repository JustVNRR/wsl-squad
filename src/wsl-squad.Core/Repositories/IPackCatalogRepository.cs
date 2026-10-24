using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public interface IPackCatalogRepository
{
    void SeedFrom(string sourceRoot);

    string GetRoot();

    IReadOnlyList<WslPack> GetAllPacks();

    void SavePackDescription(string packName, string description);

    void CopyPack(string sourceName, string newName);

    void DeletePack(string packName);

    void OpenFolder(string packName);
}
