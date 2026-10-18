using System.Globalization;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

/// <summary>Reads archives and moves their root (reference <c>ArchiveRepository</c>).</summary>
public sealed class ArchiveRepository : IArchiveRepository
{
    private readonly IConfigRepository? _configRepository;
    private readonly string? _fixedRoot;
    private readonly InstanceRepository _files;

    public ArchiveRepository(string archivesRoot)
    {
        _fixedRoot = archivesRoot;
        _files = new InstanceRepository(() => ArchivesRoot);
    }

    public ArchiveRepository(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
        _files = new InstanceRepository(() => ArchivesRoot);
    }

    private string ArchivesRoot =>
        _configRepository is not null
            ? Path.Combine(_configRepository.GetSavedRoot(), "archives")
            : _fixedRoot ?? string.Empty;

    public string GetFolder(string archiveName)
    {
        return Path.Combine(ArchivesRoot, archiveName);
    }

    public IReadOnlyList<WslInstance> GetAllArchives()
    {
        if (!Directory.Exists(ArchivesRoot))
        {
            return [];
        }

        List<WslInstance> archives = [];

        foreach (string directory in Directory.EnumerateDirectories(ArchivesRoot).Order(StringComparer.Ordinal))
        {
            WslInstance archive;

            try
            {
                archive = new WslInstance { Name = Path.GetFileName(directory), IsArchive = true };
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (GetTar(archive.Name).Length == 0)
            {
                continue;
            }

            archives.Add(archive);
        }

        return archives;
    }

    public string GetTar(string archiveName)
    {
        string folder = GetFolder(archiveName);

        if (!Directory.Exists(folder))
        {
            return string.Empty;
        }

        return Directory.EnumerateFiles(folder, "*.tar*")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTime)
            .Select(file => file.FullName)
            .FirstOrDefault() ?? string.Empty;
    }

    public long GetTarSize(string archiveName)
    {
        string tar = GetTar(archiveName);

        return tar.Length > 0 ? new FileInfo(tar).Length : 0;
    }

    public DateTime GetCreatedAt(string archiveName)
    {
        string folder = GetFolder(archiveName);

        return Directory.Exists(folder) ? Directory.GetLastWriteTime(folder) : DateTime.MinValue;
    }

    public WslInstance GetInstance(string archiveName)
    {
        WslInstance archive = _files.GetInstance(archiveName);
        archive.IsArchive = true;

        return archive;
    }

    public void SaveInstance(WslInstance archive, string docker)
    {
        _files.SaveInstance(archive, docker);
    }

    public string GetDocker(string archiveName)
    {
        return _files.GetSavedDocker(archiveName);
    }

    public void EnsureFolder(string archiveName)
    {
        _ = Directory.CreateDirectory(GetFolder(archiveName));
    }

    public bool HasArchive(string archiveName)
    {
        return Directory.Exists(GetFolder(archiveName));
    }

    public string GetIconPath(string archiveName)
    {
        return Path.Combine(GetFolder(archiveName), "terminal-icon.png");
    }

    public bool HasIcon(string archiveName)
    {
        return File.Exists(GetIconPath(archiveName));
    }

    public void SaveIcon(string archiveName, string source)
    {
        File.Copy(source, GetIconPath(archiveName), overwrite: true);
    }

    public void DeleteArchive(string archiveName)
    {
        string folder = GetFolder(archiveName);

        if (!Directory.Exists(folder))
        {
            throw new InvalidOperationException($"There is no archive named '{archiveName}': {folder} does not exist.");
        }

        Directory.Delete(folder, recursive: true);
    }

    public void MoveTo(string target)
    {
        if (!Directory.Exists(ArchivesRoot))
        {
            return;
        }

        string targetFull = Path.GetFullPath(target);
        string newArchives = Path.Combine(targetFull, "archives");

        if (Directory.Exists(newArchives))
        {
            throw new InvalidOperationException($"'{newArchives}' already exists - nothing was moved. Move it away first.");
        }

        string sourceRoot = Path.GetPathRoot(ArchivesRoot) ?? string.Empty;
        string targetRoot = Path.GetPathRoot(targetFull) ?? string.Empty;

        if (sourceRoot.TrimEnd('\\').Equals(targetRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(newArchives) ?? targetFull);
                Directory.Move(ArchivesRoot, newArchives);
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException($"the move failed: {exception.Message}", exception);
            }

            return;
        }

        long archivesSize = Directory.EnumerateFiles(ArchivesRoot, "*", SearchOption.AllDirectories)
            .Sum(path => new FileInfo(path).Length);
        long free = new DriveInfo(targetRoot).AvailableFreeSpace;

        if (free < archivesSize + (2L * 1024 * 1024 * 1024))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"not enough free space on '{targetRoot.TrimEnd('\\')}' - {(archivesSize + (2L * 1024 * 1024 * 1024) + (1024L * 1024 * 1024) - 1) / (1024L * 1024 * 1024)} GB needed."));
        }

        CopyTree(ArchivesRoot, newArchives);
    }

    private static void CopyTree(string source, string target)
    {
        _ = Directory.CreateDirectory(target);

        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            _ = Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }
}
