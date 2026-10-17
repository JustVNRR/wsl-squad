using wsl_squad.Core.Abstractions.Storage;

namespace wsl_squad.Core.Infrastructure.Storage;

/// <summary>Reports free space from the drive or mount containing a path.</summary>
public sealed class DriveInfoDiskSpaceProbe : IDiskSpaceProbe
{
    public long GetAvailableFreeBytes(string path)
    {
        string root = Path.GetPathRoot(path) ?? path;

        return new DriveInfo(root).AvailableFreeSpace;
    }
}
