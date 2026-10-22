using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public interface IArchiveRepository
{
    string GetFolder(string archiveName);

    IReadOnlyList<WslInstance> GetAllArchives();

    string GetTar(string archiveName);

    long GetTarSize(string archiveName);

    DateTime GetCreatedAt(string archiveName);

    WslInstance GetInstance(string archiveName);

    void SaveInstance(WslInstance archive, string docker);

    string GetDocker(string archiveName);

    void EnsureFolder(string archiveName);

    bool HasArchive(string archiveName);

    string GetIconPath(string archiveName);

    bool HasIcon(string archiveName);

    void SaveIcon(string archiveName, string source);

    void DeleteArchive(string archiveName);

    void MoveTo(string target);
}
