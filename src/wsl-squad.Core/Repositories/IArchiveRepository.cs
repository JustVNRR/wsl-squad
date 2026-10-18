using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

/// <summary>Archives on disk.</summary>
public interface IArchiveRepository
{
    public string GetFolder(string archiveName);

    public IReadOnlyList<WslInstance> GetAllArchives();

    public string GetTar(string archiveName);

    public long GetTarSize(string archiveName);

    public DateTime GetCreatedAt(string archiveName);

    public WslInstance GetInstance(string archiveName);

    public void SaveInstance(WslInstance archive, string docker);

    public string GetDocker(string archiveName);

    public void EnsureFolder(string archiveName);

    public bool HasArchive(string archiveName);

    public string GetIconPath(string archiveName);

    public bool HasIcon(string archiveName);

    public void SaveIcon(string archiveName, string source);

    public void DeleteArchive(string archiveName);

    public void MoveTo(string target);
}
