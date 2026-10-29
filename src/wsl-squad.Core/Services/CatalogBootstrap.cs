using WslSquad.Core.Exceptions;
using System.Reflection;

namespace WslSquad.Core.Services;

public static class CatalogBootstrap
{
    private const string _versionFileName = "assets.version";

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
        Directory.CreateDirectory(assetsRoot);

        Assembly assembly = typeof(ServiceFactory).Assembly;
        string embeddedVersion = EmbeddedVersion(assembly);
        string versionPath = Path.Combine(assetsRoot, _versionFileName);
        string? storedVersion = File.Exists(versionPath)
            ? File.ReadAllText(versionPath).Trim()
            : null;
        bool refresh = !string.Equals(storedVersion, embeddedVersion, StringComparison.Ordinal);
        string backupRoot = Path.Combine(
            assetsRoot,
            $"backup-{SafeName(storedVersion ?? "unversioned")}");

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!IsAsset(resource))
            {
                continue;
            }

            string target = Path.Combine(
                assetsRoot,
                resource.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(target))
            {
                if (!refresh)
                {
                    continue;
                }

                Backup(target, assetsRoot, backupRoot);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target) ?? assetsRoot);

            using Stream source = assembly.GetManifestResourceStream(resource)!;
            using FileStream destination = File.Create(target);
            source.CopyTo(destination);
        }

        if (refresh || !File.Exists(versionPath))
        {
            File.WriteAllText(versionPath, embeddedVersion);
        }
    }

    private static bool IsAsset(string resource)
    {
        return resource.StartsWith("packs/", StringComparison.Ordinal) ||
            resource.StartsWith("ingredients/", StringComparison.Ordinal) ||
            resource.StartsWith("recipes/", StringComparison.Ordinal) ||
            resource.StartsWith("fonts/", StringComparison.Ordinal) ||
            resource.StartsWith("languages/", StringComparison.Ordinal);
    }

    private static string EmbeddedVersion(Assembly assembly)
    {
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            int metadata = informational.IndexOf('+', StringComparison.Ordinal);
            return metadata >= 0 ? informational[..metadata] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }

    private static string SafeName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] safe = [.. value.Select(character => invalid.Contains(character) ? '_' : character)];
        return safe.Length == 0 ? "unversioned" : new string(safe);
    }

    private static void Backup(string target, string assetsRoot, string backupRoot)
    {
        string relative = Path.GetRelativePath(assetsRoot, target);
        string backup = Path.Combine(backupRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(backup) ?? backupRoot);
        File.Copy(target, backup, overwrite: true);
    }
}
