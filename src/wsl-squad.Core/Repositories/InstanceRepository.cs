using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public sealed class InstanceRepository : IInstanceRepository
{
    private readonly Func<string> _root;

    public InstanceRepository(string instancesRoot)
        : this(() => instancesRoot)
    {
    }

    public InstanceRepository(IConfigRepository configRepository)
        : this(configRepository.GetSavedRoot)
    {
    }

    public InstanceRepository(Func<string> root)
    {
        _root = root;
    }

    public string GetFolder(string name)
    {
        return Path.Combine(_root(), name);
    }

    public WslInstance GetInstance(string name)
    {
        WslInstance instance = new() { Name = name };
        InstanceFile? saved = AtomicFile.ReadJson<InstanceFile>(Path.Combine(GetFolder(name), "instance.json"));

        if (saved is null)
        {
            return instance;
        }

        if (!string.IsNullOrEmpty(saved.DefaultUser))
        {
            instance.DefaultUser = saved.DefaultUser;
        }

        if (saved.Theme is not null)
        {
            WslTheme theme = new()
            {
                FontName = Empty(saved.Theme.Font),
                ColorScheme = Empty(saved.Theme.ColorScheme),
                IconPath = Empty(saved.Theme.IconFrom),
            };

            if (!string.IsNullOrEmpty(saved.Theme.IconText))
            {
                theme.SetIcon(new IconDrawing(
                    saved.Theme.IconText,
                    saved.Theme.IconTop,
                    saved.Theme.IconBottom,
                    saved.Theme.IconTextColor));
            }

            instance.Theme = theme;
        }

        return instance;
    }

    public void SaveInstance(WslInstance instance, string? docker)
    {
        ArgumentNullException.ThrowIfNull(instance);

        WslTheme theme = instance.Theme ?? new WslTheme();
        IconDrawing? icon = theme.Icon;
        ThemeFile block = new()
        {
            Font = Empty(theme.FontName),
            ColorScheme = Empty(theme.ColorScheme),
            IconFrom = Empty(theme.IconPath),
        };

        if (!string.IsNullOrEmpty(icon?.Text))
        {
            block.IconText = icon.Text;
            block.IconTop = icon.Top;
            block.IconBottom = icon.Bottom;
            block.IconTextColor = icon.TextColor;
        }

        AtomicFile.WriteJson(
            Path.Combine(GetFolder(instance.Name), "instance.json"),
            new InstanceFile
            {
                DefaultUser = Empty(instance.DefaultUser),
                Theme = block,
                Docker = string.IsNullOrEmpty(docker) ? null : docker,
            });
    }

    public string GetSavedDocker(string name)
    {
        InstanceFile? saved = AtomicFile.ReadJson<InstanceFile>(Path.Combine(GetFolder(name), "instance.json"));

        return saved?.Docker ?? string.Empty;
    }

    public void SaveIcon(string name, string source)
    {
        string target = Path.Combine(GetFolder(name), "terminal-icon.png");

        if (string.Equals(
            Path.GetFullPath(source),
            Path.GetFullPath(target),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            File.Copy(source, target, overwrite: true);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"The icon could not be copied: {exception.Message}", exception);
        }
    }

    public void WriteMarker(string name, string by)
    {
        string folder = GetFolder(name);

        if (!Directory.Exists(folder))
        {
            return;
        }

        AtomicFile.WriteJson(Path.Combine(folder, ".wsl-squad"), new MarkerFile
        {
            Template = "wsl-squad",
            Created = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            By = by,
        });
    }

    public bool HasMarker(string name)
    {
        return HasMarkerIn(GetFolder(name));
    }

    public bool HasMarkerIn(string folder)
    {
        return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, ".wsl-squad"));
    }

    public IReadOnlyList<string> GetMarkedFolders()
    {
        string root = _root();

        if (!Directory.Exists(root))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateDirectories(root)
                .Where(directory => File.Exists(Path.Combine(directory, ".wsl-squad")))
                .Select(directory => Path.GetFileName(directory))
                .Order(StringComparer.Ordinal),
        ];
    }

    public long GetVhdxSize(string name)
    {
        string vhdx = Path.Combine(GetFolder(name), "ext4.vhdx");

        return File.Exists(vhdx) ? new FileInfo(vhdx).Length : 0;
    }

    public string GetIconPath(string name)
    {
        return Path.Combine(GetFolder(name), "terminal-icon.png");
    }

    public bool HasIcon(string name)
    {
        return File.Exists(GetIconPath(name));
    }

    public bool IsFolderOccupied(string name)
    {
        string folder = GetFolder(name);

        return Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any();
    }

    public void RemoveFolder(string name)
    {
        string folder = GetFolder(name);

        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[InstanceRepository] Could not remove instance folder '{folder}': {ex.Message}");
        }
    }

    public void EnsureFolder(string folder)
    {
        _ = Directory.CreateDirectory(folder);
    }

    public long GetFreeSpace(string path)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(path))
            ?? throw new InvalidOperationException($"'{path}' has no volume.");

        return new DriveInfo(root).AvailableFreeSpace;
    }

    public DateTime GetCreatedAt(string name)
    {
        string folder = GetFolder(name);

        return Directory.Exists(folder) ? Directory.GetCreationTime(folder) : DateTime.MinValue;
    }

    private static string Empty(string? value)
    {
        return value ?? string.Empty;
    }

    private sealed class InstanceFile
    {
        public string DefaultUser { get; set; } = string.Empty;

        public ThemeFile? Theme { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Docker { get; set; }
    }

    private sealed class ThemeFile
    {
        public string Font { get; set; } = string.Empty;

        public string ColorScheme { get; set; } = string.Empty;

        public string IconFrom { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconText { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconTop { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconBottom { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconTextColor { get; set; }
    }

    private sealed class MarkerFile
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("created")]
        public string? Created { get; set; }

        [JsonPropertyName("by")]
        public string? By { get; set; }
    }
}
