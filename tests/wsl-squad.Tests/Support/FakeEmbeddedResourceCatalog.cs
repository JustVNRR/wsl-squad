using System.Text;

using wsl_squad.Core.Abstractions.Resources;

namespace wsl_squad.Tests.Support;

public sealed class FakeEmbeddedResourceCatalog : IEmbeddedResourceCatalog
{
    private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);

    public void Add(string logicalName, string content)
    {
        _resources[logicalName] = Encoding.UTF8.GetBytes(content);
    }

    public IReadOnlyDictionary<string, byte[]> ReadAll()
    {
        return _resources;
    }
}
