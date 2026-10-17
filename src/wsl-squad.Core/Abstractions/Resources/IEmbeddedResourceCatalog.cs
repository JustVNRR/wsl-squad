namespace wsl_squad.Core.Abstractions.Resources;

/// <summary>Provides embedded resources keyed by their logical names.</summary>
public interface IEmbeddedResourceCatalog
{
    public IReadOnlyDictionary<string, byte[]> ReadAll();
}
