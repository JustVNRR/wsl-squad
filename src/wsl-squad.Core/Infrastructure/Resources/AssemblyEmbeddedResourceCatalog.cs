using System.Reflection;

using wsl_squad.Core.Abstractions.Resources;

namespace wsl_squad.Core.Infrastructure.Resources;

/// <summary>Reads logical resources from an assembly manifest.</summary>
public sealed class AssemblyEmbeddedResourceCatalog : IEmbeddedResourceCatalog
{
    private readonly Assembly _assembly;

    public AssemblyEmbeddedResourceCatalog(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assembly = assembly;
    }

    public IReadOnlyDictionary<string, byte[]> ReadAll()
    {
        Dictionary<string, byte[]> resources = new(StringComparer.Ordinal);

        foreach (string name in _assembly.GetManifestResourceNames())
        {
            using Stream? stream = _assembly.GetManifestResourceStream(name);

            if (stream is null)
            {
                continue;
            }

            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            resources[name.Replace('\\', '/')] = buffer.ToArray();
        }

        return resources;
    }
}
