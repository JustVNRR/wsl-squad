using WslSquad.Core.Exceptions;
using System.Reflection;

namespace WslSquad.Core.Services;

public static class CatalogBootstrap
{
    public static string ReadText(string logicalName)
    {
        Assembly assembly = typeof(ServiceFactory).Assembly;
        using Stream source = assembly.GetManifestResourceStream(logicalName)
            ?? throw new WslSquadException(WslSquadErrorCode.EmbeddedResourceMissing, logicalName);
        using StreamReader reader = new(source);

        return reader.ReadToEnd();
    }

    public static void Ensure(string assetsRoot)
    {
        Assembly assembly = typeof(ServiceFactory).Assembly;

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith("packs/", StringComparison.Ordinal) &&
                !resource.StartsWith("ingredients/", StringComparison.Ordinal) &&
                !resource.StartsWith("recipes/", StringComparison.Ordinal) &&
                !resource.StartsWith("fonts/", StringComparison.Ordinal) &&
                !resource.StartsWith("languages/", StringComparison.Ordinal))
            {
                continue;
            }

            string target = Path.Combine(
                assetsRoot,
                resource.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(target))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target) ?? assetsRoot);

            using Stream source = assembly.GetManifestResourceStream(resource)!;
            using FileStream destination = File.Create(target);
            source.CopyTo(destination);
        }
    }
}
