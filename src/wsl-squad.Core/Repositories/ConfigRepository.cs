using wsl_squad.Core.Exceptions;
using System.Diagnostics;
using System.Security.Cryptography;

namespace wsl_squad.Core.Repositories;

public sealed record SourceRow(string Name, string Path, DateTime Modified);

public sealed class ConfigRepository : IConfigRepository
{
    private static readonly string[] _recipeKinds = ["dockerfiles", "onboardings", "dockerimages", "recipes"];

    private readonly string _assetsDir;
    private readonly string _distroTree;

    public ConfigRepository(string assetsDir)
        : this(assetsDir, string.Empty)
    {
    }

    public ConfigRepository(string assetsDir, string distroTree)
    {
        _assetsDir = assetsDir;
        _distroTree = distroTree;
    }

    public IReadOnlyList<string> GetThemeNames()
    {
        string root = Path.Combine(_assetsDir, "themes");

        return Directory.Exists(root)
            ? [.. Directory.EnumerateFiles(root, "*.xaml").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)!]
            : [];
    }

    public IReadOnlyList<string> GetFontNames()
    {
        string root = Path.Combine(_assetsDir, "fonts");

        return Directory.Exists(root) ? ReadFontNames(root) : [];
    }

    public IReadOnlyList<string> GetPromptFontNames()
    {
        string root = Path.Combine(_assetsDir, "fonts");

        if (!Directory.Exists(root))
        {
            return [];
        }

        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            string? file = Directory.EnumerateFiles(directory)
                .FirstOrDefault(path =>
                    Path.GetExtension(path).Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(path).Equals(".otf", StringComparison.OrdinalIgnoreCase));

            if (file is null || !FontGlyphReader.HasIconGlyph(file))
            {
                continue;
            }

            foreach (string fontFile in Directory.EnumerateFiles(directory))
            {
                string extension = Path.GetExtension(fontFile);

                if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
                    !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? family = FontNameReader.ReadFamily(File.ReadAllBytes(fontFile));

                if (!string.IsNullOrEmpty(family) &&
                    !family.StartsWith("Font Awesome", StringComparison.OrdinalIgnoreCase))
                {
                    _ = names.Add(family);
                }
            }
        }

        return [.. names.Order(StringComparer.OrdinalIgnoreCase)];
    }

    public IReadOnlyList<SourceRow> GetDockerfiles()
    {
        List<SourceRow> rows = ReadDockerfileRows();

        if (rows.Count == 0)
        {
            WriteSeedRecipe("dockerfiles", "Dockerfile", "Dockerfile", "Dockerfile.dockerignore");
            rows = ReadDockerfileRows();
        }

        return rows;
    }

    public IReadOnlyList<SourceRow> GetOnboardings()
    {
        List<SourceRow> rows = ReadSourceRows("onboardings", "onboarding.sh");

        if (rows.Count == 0)
        {
            WriteSeedRecipe("onboardings", "onboarding", "onboarding.sh", string.Empty);
            rows = ReadSourceRows("onboardings", "onboarding.sh");
        }

        return rows;
    }

    public IReadOnlyList<SourceRow> GetRecipes()
    {
        string root = Path.Combine(_assetsDir, "recipes");

        if (!Directory.Exists(root))
        {
            return [];
        }

        List<SourceRow> rows = [];

        foreach (string slot in Directory.EnumerateDirectories(root))
        {
            rows.Add(new SourceRow(
                Path.GetFileName(slot),
                slot,
                Directory.GetLastWriteTime(slot)));
        }

        return rows;
    }

    public string CreateRecipe(string name)
    {
        string fingerprint = Guid.NewGuid().ToString("N")[..8];
        string slot = Path.Combine(_assetsDir, "recipes", $"{name}-{fingerprint}");

        _ = Directory.CreateDirectory(slot);

        return slot;
    }

    public string CopyRecipe(string kind, string source, string fileName)
    {
        string fingerprint = ShortHash(source);
        string slot = Path.Combine(
            _assetsDir,
            kind,
            $"{Path.GetFileNameWithoutExtension(source)}-{fingerprint}");

        _ = Directory.CreateDirectory(slot);
        string target = Path.Combine(slot, fileName);
        File.Copy(source, target, overwrite: true);
        File.SetLastWriteTime(target, DateTime.Now);

        if (fileName == "Dockerfile")
        {
            string sibling = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, ".dockerignore");

            if (File.Exists(sibling))
            {
                File.Copy(sibling, Path.Combine(slot, "Dockerfile.dockerignore"), overwrite: true);
            }
        }

        return target;
    }

    public void RemoveRecipe(string path)
    {
        string slot = Path.GetFullPath(path);

        if (!Directory.Exists(slot))
        {
            slot = Path.GetFullPath(Path.GetDirectoryName(path) ?? slot);
        }

        bool under = _recipeKinds
            .Select(kind => Path.GetFullPath(Path.Combine(_assetsDir, kind)))
            .Any(root => slot.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal));

        if (!under)
        {
            throw new WslSquadException(WslSquadErrorCode.RecipeSlotNotFound, slot);
        }

        Directory.Delete(slot, recursive: true);
    }

    public void ExportRecipe(string path, string target)
    {
        File.Copy(path, target, overwrite: true);
    }

    public string CopyFont(string source)
    {
        string fingerprint = ShortHash(source);
        string slot = Path.Combine(
            _assetsDir,
            "fonts",
            $"{Path.GetFileNameWithoutExtension(source)}-{fingerprint}");

        _ = Directory.CreateDirectory(slot);
        File.Copy(source, Path.Combine(slot, Path.GetFileName(source)), overwrite: true);

        return slot;
    }

    public string CopyTheme(string source)
    {
        string name = Path.GetFileNameWithoutExtension(source);
        string target = Path.Combine(_assetsDir, "themes", $"{name}.xaml");

        _ = Directory.CreateDirectory(Path.GetDirectoryName(target) ?? string.Empty);
        File.Copy(source, target, overwrite: true);

        return target;
    }

    public void DeleteTheme(string name)
    {
        string target = Path.Combine(_assetsDir, "themes", $"{Path.GetFileNameWithoutExtension(name)}.xaml");

        if (!File.Exists(target))
        {
            throw new WslSquadException(WslSquadErrorCode.ThemeNotFound, name);
        }

        File.Delete(target);
    }

    public void OpenTheme(string name)
    {
        string target = Path.Combine(_assetsDir, "themes", $"{Path.GetFileNameWithoutExtension(name)}.xaml");

        if (!File.Exists(target))
        {
            throw new WslSquadException(WslSquadErrorCode.ThemeNotFound, name);
        }

        OpenWithShell(target);
    }

    public string GetSavedRoot()
    {
        string file = RootFile();

        if (file.Length == 0 || !File.Exists(file))
        {
            return string.Empty;
        }

        string? line = File.ReadLines(file).FirstOrDefault();

        return line?.Trim().TrimEnd('\\') ?? string.Empty;
    }

    public void SaveRoot(string root)
    {
        string file = RootFile();

        if (file.Length == 0)
        {
            return;
        }

        _ = Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
        AtomicFile.WriteText(file, root.TrimEnd('\\'));
    }

    public string EnsureWslConfig()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig");

        if (!File.Exists(path))
        {
            File.WriteAllText(
                path,
                """
                # WSL's Windows-wide settings. Read when the WSL machine starts: `wsl --shutdown`
                # then a new start applies a change.
                #
                # The common keys: https://learn.microsoft.com/windows/wsl/wsl-config
                #
                # [wsl2]
                # memory=8GB          # the machine's memory cap
                # processors=4        # the CPUs it may use
                # dnsTunneling=true   # WSL answers the DNS itself
                """);
        }

        return path;
    }

    private List<SourceRow> ReadDockerfileRows()
    {
        return ReadSourceRows("dockerfiles", "Dockerfile");
    }

    private static IReadOnlyList<string> ReadFontNames(string root)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                string extension = Path.GetExtension(file);

                if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
                    !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? family = FontNameReader.ReadFamily(File.ReadAllBytes(file));

                if (string.IsNullOrEmpty(family) ||
                    family.StartsWith("Font Awesome", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _ = names.Add(family);
            }
        }

        return [.. names.Order(StringComparer.Ordinal)];
    }

    private List<SourceRow> ReadSourceRows(string folder, string file)
    {
        string root = Path.Combine(_assetsDir, folder);

        if (!Directory.Exists(root))
        {
            return [];
        }

        List<SourceRow> rows = [];

        foreach (string slot in Directory.EnumerateDirectories(root))
        {
            string path = Path.Combine(slot, file);

            if (!File.Exists(path))
            {
                continue;
            }

            rows.Add(new SourceRow(
                Path.GetFileName(slot),
                path,
                File.GetLastWriteTime(path)));
        }

        return rows;
    }

    private void WriteSeedRecipe(string kind, string slot, string fileName, string sibling)
    {
        if (_distroTree.Length == 0)
        {
            return;
        }

        string source = Path.Combine(_distroTree, "ingredients", fileName);

        if (!File.Exists(source))
        {
            return;
        }

        string targetDirectory = Path.Combine(_assetsDir, kind, slot);
        _ = Directory.CreateDirectory(targetDirectory);

        string target = Path.Combine(targetDirectory, fileName);
        File.Copy(source, target, overwrite: true);
        File.SetLastWriteTime(target, DateTime.Now);

        if (sibling.Length > 0)
        {
            string beside = Path.Combine(Path.GetDirectoryName(source) ?? string.Empty, sibling);

            if (File.Exists(beside))
            {
                File.Copy(beside, Path.Combine(targetDirectory, sibling), overwrite: true);
            }
        }
    }

    private static string ShortHash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);

        return Convert.ToHexStringLower(hash)[..8];
    }

    private static void OpenWithShell(string path)
    {
        _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static string RootFile()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return local.Length == 0 ? string.Empty : Path.Combine(local, "wsl-squad", "root.txt");
    }
}
