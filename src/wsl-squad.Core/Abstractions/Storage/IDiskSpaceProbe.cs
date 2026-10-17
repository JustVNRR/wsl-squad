namespace wsl_squad.Core.Abstractions.Storage;

/// <summary>Reports available disk space for a path.</summary>
public interface IDiskSpaceProbe
{
    public long GetAvailableFreeBytes(string path);
}
