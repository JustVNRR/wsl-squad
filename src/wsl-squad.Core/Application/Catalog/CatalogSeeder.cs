using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;

using wsl_squad.Core.Abstractions.Resources;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Catalog;

/// <summary>Result of seeding the editable catalog from embedded resources.</summary>
public sealed record CatalogSeedResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Skipped);

/// <summary>Copies embedded resources into the editable catalog without overwriting user edits.</summary>
public sealed class CatalogSeeder
{
    public const string ManifestFileName = ".seed-manifest.json";

    private readonly IFileSystem _fileSystem;
    private readonly IEmbeddedResourceCatalog _resources;

    public CatalogSeeder(IFileSystem fileSystem, IEmbeddedResourceCatalog resources)
    {
        _fileSystem = fileSystem;
        _resources = resources;
    }

    public OperationResult<CatalogSeedResult> Seed(string catalogRoot)
    {
        if (!PathHelper.IsFullyQualified(catalogRoot))
        {
            return OperationResult<CatalogSeedResult>.Fail(
                ErrorCode.Validation,
                $"Catalog root '{catalogRoot}' is not an absolute path.");
        }

        List<string> created = [];
        List<string> updated = [];
        List<string> skipped = [];
        Dictionary<string, string> manifest = LoadManifest(catalogRoot);

        try
        {
            foreach (KeyValuePair<string, byte[]> resource in _resources.ReadAll())
            {
                string? relativePath = MapToRelativePath(resource.Key);

                if (relativePath is null)
                {
                    continue;
                }

                string targetPath = _fileSystem.Path.Combine(catalogRoot, relativePath);
                string embeddedHash = Hash(resource.Value);

                if (!_fileSystem.File.Exists(targetPath))
                {
                    AtomicFileWriter.WriteAllBytes(_fileSystem, targetPath, resource.Value);
                    manifest[relativePath] = embeddedHash;
                    created.Add(relativePath);
                    continue;
                }

                string existingHash = Hash(_fileSystem.File.ReadAllBytes(targetPath));

                if (!manifest.TryGetValue(relativePath, out string? seededHash) || seededHash != existingHash)
                {
                    skipped.Add(relativePath);
                    continue;
                }

                if (existingHash != embeddedHash)
                {
                    AtomicFileWriter.WriteAllBytes(_fileSystem, targetPath, resource.Value);
                    manifest[relativePath] = embeddedHash;
                    updated.Add(relativePath);
                }
            }

            SaveManifest(catalogRoot, manifest);

            return OperationResult<CatalogSeedResult>.Ok(new CatalogSeedResult(created, updated, skipped));
        }
        catch (IOException exception)
        {
            return OperationResult<CatalogSeedResult>.Fail(
                ErrorCode.IoError,
                $"Catalog '{catalogRoot}' could not be seeded.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<CatalogSeedResult>.Fail(
                ErrorCode.PermissionDenied,
                $"Catalog '{catalogRoot}' could not be seeded.",
                exception.Message);
        }
    }

    internal static string? MapToRelativePath(string logicalName)
    {
        if (string.Equals(logicalName, "linux/build/Dockerfile", StringComparison.Ordinal))
        {
            return "dockerfiles/default/Dockerfile";
        }

        if (string.Equals(logicalName, "linux/build/onboarding.sh", StringComparison.Ordinal))
        {
            return "onboardings/default/onboarding.sh";
        }

        const string packsPrefix = "linux/packs/";

        if (logicalName.StartsWith(packsPrefix, StringComparison.Ordinal))
        {
            return "packs/" + logicalName[packsPrefix.Length..];
        }

        if (logicalName.StartsWith("recipes/", StringComparison.Ordinal) ||
            logicalName.StartsWith("fonts/", StringComparison.Ordinal))
        {
            return logicalName;
        }

        return null;
    }

    private Dictionary<string, string> LoadManifest(string catalogRoot)
    {
        string manifestPath = _fileSystem.Path.Combine(catalogRoot, ManifestFileName);

        if (!_fileSystem.File.Exists(manifestPath))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            Dictionary<string, string>? manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(
                _fileSystem.File.ReadAllText(manifestPath),
                JsonDefaults.Options);

            return manifest is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(manifest, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (IOException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private void SaveManifest(string catalogRoot, Dictionary<string, string> manifest)
    {
        string manifestPath = _fileSystem.Path.Combine(catalogRoot, ManifestFileName);
        AtomicFileWriter.WriteAllText(
            _fileSystem,
            manifestPath,
            JsonSerializer.Serialize(manifest, JsonDefaults.Options));
    }

    private static string Hash(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }
}
