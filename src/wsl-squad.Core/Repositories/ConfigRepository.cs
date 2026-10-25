using WslSquad.Core.Exceptions;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using WslSquad.Core.Infrastructure;

namespace WslSquad.Core.Repositories;

public sealed record SourceRow(string Name, string Path);

public sealed class ConfigRepository : IConfigRepository
{
    private static readonly string[] _recipeKinds = ["recipes"];

    private readonly string _assetsDir;

    public ConfigRepository(string assetsDir)
    {
        _assetsDir = assetsDir;
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
                    names.Add(family);
                }
            }
        }

        return [.. names.Order(StringComparer.OrdinalIgnoreCase)];
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
                slot));
        }

        return rows;
    }

    public string CreateRecipe(string name)
    {
        string slot = RecipeSlot(name);

        if (Directory.Exists(slot) || File.Exists(slot))
        {
            throw new WslSquadException(WslSquadErrorCode.RecipeAlreadyExists, name);
        }

        Directory.CreateDirectory(slot);

        return slot;
    }

    public string DuplicateRecipe(string sourcePath, string name)
    {
        string source = Path.GetFullPath(sourcePath);
        string target = RecipeSlot(name);

        if (!Directory.Exists(source) ||
            !source.StartsWith(
                RecipesRoot().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new WslSquadException(WslSquadErrorCode.RecipeSlotNotFound, source);
        }

        if (Directory.Exists(target) || File.Exists(target))
        {
            throw new WslSquadException(WslSquadErrorCode.RecipeAlreadyExists, name);
        }

        CopyTree(source, target);

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

        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
        AtomicFile.WriteText(file, root.TrimEnd('\\'));
    }

    public string GetLanguage()
    {
        string? text = AtomicFile.ReadText(ConfigFile());

        if (text is null)
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(text, _documentOptions);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "language", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString()?.Trim() ?? string.Empty;
                }
            }
        }
        catch (JsonException)
        {
            return string.Empty;
        }

        return string.Empty;
    }

    public void SaveLanguage(string code)
    {
        JsonObject root;

        try
        {
            string? text = AtomicFile.ReadText(ConfigFile());
            root = text is null
                ? []
                : JsonNode.Parse(text, documentOptions: _documentOptions) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            root = [];
        }

        root["language"] = code;

        try
        {
            AtomicFile.WriteText(ConfigFile(), root.ToJsonString(AtomicFile.JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[ConfigRepository] Could not save the language preference: {exception.Message}");
        }
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

                names.Add(family);
            }
        }

        return [.. names.Order(StringComparer.Ordinal)];
    }

    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private string RecipesRoot()
    {
        return Path.GetFullPath(Path.Combine(_assetsDir, "recipes"));
    }

    private string RecipeSlot(string name)
    {
        return Path.Combine(RecipesRoot(), name);
    }

    private static void CopyTree(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    private string ConfigFile()
    {
        return Path.Combine(_assetsDir, "config.json");
    }

    private string RootFile()
    {
        return Path.Combine(_assetsDir, "root.txt");
    }
}
