using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Tests.Support;

public sealed class FakeWslRegistryGateway : IWslRegistryGateway
{
    private readonly List<WslRegistryEntry> _entries = [];

    public bool IsSupported { get; set; } = true;

    public void Add(string name, string basePath)
    {
        _entries.Add(new WslRegistryEntry(name, basePath));
    }

    public IReadOnlyList<WslRegistryEntry> GetDistributions()
    {
        return _entries;
    }
}
