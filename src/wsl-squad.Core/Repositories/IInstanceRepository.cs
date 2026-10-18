using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

/// <summary>An instance's folder and metadata.</summary>
public interface IInstanceRepository
{
    public string GetFolder(string name);

    public WslInstance GetInstance(string name);

    public void SaveInstance(WslInstance instance, string? docker);

    public string GetSavedDocker(string name);

    public void SaveIcon(string name, string source);

    public void WriteMarker(string name, string by);

    public bool HasMarker(string name);

    public bool HasMarkerIn(string folder);

    public IReadOnlyList<string> GetMarkedFolders();

    public long GetVhdxSize(string name);

    public string GetIconPath(string name);

    public bool HasIcon(string name);

    public bool IsFolderOccupied(string name);

    public void RemoveFolder(string name);

    public void EnsureFolder(string folder);

    public long GetFreeSpace(string path);

    public DateTime GetCreatedAt(string name);
}
