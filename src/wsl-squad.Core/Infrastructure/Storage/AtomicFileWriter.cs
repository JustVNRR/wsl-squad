using System.IO.Abstractions;

namespace wsl_squad.Core.Infrastructure.Storage;

/// <summary>Writes files through a temporary sibling and an overwriting move.</summary>
public static class AtomicFileWriter
{
    public static void WriteAllText(IFileSystem fileSystem, string path, string content)
    {
        string? directory = fileSystem.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            _ = fileSystem.Directory.CreateDirectory(directory);
        }

        string temporaryPath = fileSystem.Path.Combine(
            directory ?? string.Empty,
            $".{fileSystem.Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        fileSystem.File.WriteAllText(temporaryPath, content);
        fileSystem.File.Move(temporaryPath, path, overwrite: true);
    }

    public static void WriteAllBytes(IFileSystem fileSystem, string path, byte[] content)
    {
        string? directory = fileSystem.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            _ = fileSystem.Directory.CreateDirectory(directory);
        }

        string temporaryPath = fileSystem.Path.Combine(
            directory ?? string.Empty,
            $".{fileSystem.Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        fileSystem.File.WriteAllBytes(temporaryPath, content);
        fileSystem.File.Move(temporaryPath, path, overwrite: true);
    }
}
