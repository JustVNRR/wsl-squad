using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public interface IInstanceRepository
{
    string GetFolder(string name);

    WslInstance GetInstance(string name);

    void SaveInstance(WslInstance instance, string? docker);

    string GetSavedDocker(string name);

    void SaveIcon(string name, string source);

    void WriteMarker(string name, string by);

    bool HasMarker(string name);

    bool HasMarkerIn(string folder);

    IReadOnlyList<string> GetMarkedFolders();

    long GetVhdxSize(string name);

    string GetIconPath(string name);

    bool HasIcon(string name);

    bool IsFolderOccupied(string name);

    void RemoveFolder(string name);

    void EnsureFolder(string folder);

    long GetFreeSpace(string path);

    DateTime GetCreatedAt(string name);
}
