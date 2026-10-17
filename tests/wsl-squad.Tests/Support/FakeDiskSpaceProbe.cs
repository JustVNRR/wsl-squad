using wsl_squad.Core.Abstractions.Storage;

namespace wsl_squad.Tests.Support;

public sealed class FakeDiskSpaceProbe : IDiskSpaceProbe
{
    public long AvailableBytes { get; set; } = long.MaxValue;

    public long GetAvailableFreeBytes(string path)
    {
        return AvailableBytes;
    }
}
