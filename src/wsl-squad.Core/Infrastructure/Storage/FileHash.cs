using System.IO.Abstractions;
using System.Security.Cryptography;

namespace wsl_squad.Core.Infrastructure.Storage;

/// <summary>Computes file digests.</summary>
public static class FileHash
{
    public static string Sha256(IFileSystem fileSystem, string path)
    {
        using Stream stream = fileSystem.File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
