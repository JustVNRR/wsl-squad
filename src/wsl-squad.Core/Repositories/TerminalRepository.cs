using WslSquad.Core.Exceptions;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Win32;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public sealed record WslGuids(IReadOnlyList<string> Guids, string? ProfileGuid);

public sealed record PruneReport(int Pruned, IReadOnlyList<string> Unreadable);

public sealed record TerminalFont(string Name, bool Icons);

public sealed partial class TerminalRepository : ITerminalRepository
{
    private static readonly IconPair[] _palette =
    [
        new("orange", "#CF7040", "#B95E30", "#FFFFFF"),
        new("blue", "#3B82F6", "#2563EB", "#FFFFFF"),
        new("green", "#2E8B57", "#1F5C3E", "#FFFFFF"),
        new("purple", "#7C5CBF", "#5B3F9E", "#FFFFFF"),
        new("red", "#C0453B", "#93291F", "#FFFFFF"),
        new("teal", "#148F8A", "#0E6B67", "#FFFFFF"),
        new("graphite", "#4B5563", "#374151", "#FFFFFF"),
        new("sand", "#E3C567", "#C9A73F", "#2A2410"),
    ];

    private static readonly string[] _settingsPaths =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages",
            "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
            "LocalState",
            "settings.json"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages",
            "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe",
            "LocalState",
            "settings.json"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows Terminal",
            "settings.json"),
    ];

    private readonly string _assetsDir;

    public TerminalRepository(string assetsDir)
    {
        _assetsDir = assetsDir;
    }

    public string GetFragmentPath(string instanceName)
    {
        return Path.Combine(FragmentsFolder(), $"{instanceName}.json");
    }

    public void SaveFragment(string instanceName, string profileGuid, WslTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        string folder = FragmentsFolder();
        Directory.CreateDirectory(folder);

        JsonObject profile = new()
        {
            ["updates"] = profileGuid,
        };

        if (!string.IsNullOrEmpty(theme.IconPath) && File.Exists(theme.IconPath))
        {
            profile["icon"] = theme.IconPath;
        }

        profile["font"] = new JsonObject { ["face"] = theme.FontName ?? string.Empty };
        profile["colorScheme"] = theme.ColorScheme ?? string.Empty;
        profile["suppressApplicationTitle"] = true;
        profile["closeOnExit"] = "always";

        JsonObject fragment = new()
        {
            ["profiles"] = new JsonArray(profile),
        };

        AtomicFile.WriteText(GetFragmentPath(instanceName), fragment.ToJsonString(AtomicFile.JsonOptions));
    }

    public void DeleteFragment(string instanceName)
    {
        string path = GetFragmentPath(instanceName);

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[TerminalRepository] Could not delete fragment '{path}': {ex.Message}");
        }
    }

    public string GetProfileGuid(string instanceName)
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            string guid = ReadProfileGuid(instanceName);

            if (guid.Length > 0)
            {
                return guid;
            }

            Thread.Sleep(1000);
        }

        return string.Empty;
    }

    public bool HasWslProfile(string instanceName)
    {
        return ReadProfileGuid(instanceName).Length > 0;
    }

    public WslGuids GetWslGuids(string instanceName)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows Terminal",
            "Fragments",
            "Microsoft.WSL");

        List<string> live = [];
        string match = string.Empty;

        if (Directory.Exists(folder))
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*.json")
                         .OrderByDescending(File.GetLastWriteTime))
            {
                JsonObject? parsed = ReadTerminalJson(file);
                JsonArray? profiles = parsed?["profiles"] as JsonArray;

                if (profiles is null)
                {
                    continue;
                }

                foreach (JsonNode? entry in profiles)
                {
                    if (entry is not JsonObject profile)
                    {
                        continue;
                    }

                    string? guid = StringOrNull(profile["guid"]);

                    if (!string.IsNullOrEmpty(guid))
                    {
                        live.Add(guid);
                    }

                    if (match.Length == 0 &&
                        !string.IsNullOrEmpty(instanceName) &&
                        StringOrNull(profile["name"]) == instanceName &&
                        !string.IsNullOrEmpty(guid))
                    {
                        match = guid;
                    }
                }
            }
        }

        return new WslGuids(live, match.Length == 0 ? null : match);
    }

    public PruneReport PruneGhostEntries(string instanceName, IReadOnlyList<string> liveGuids)
    {
        int pruned = 0;
        List<string> unreadable = [];

        foreach (string settingsPath in _settingsPaths)
        {
            if (!File.Exists(settingsPath))
            {
                continue;
            }

            try
            {
                JsonObject settings = ParseObject(File.ReadAllText(settingsPath));
                JsonArray? all = settings["profiles"]?["list"] as JsonArray;

                if (all is null)
                {
                    continue;
                }

                List<JsonNode?> kept = [];

                foreach (JsonNode? node in all)
                {
                    if (node is JsonObject profile &&
                        StringOrNull(profile["source"]) == "Microsoft.WSL" &&
                        StringOrNull(profile["name"]) == instanceName &&
                        !liveGuids.Contains(StringOrNull(profile["guid"]) ?? string.Empty))
                    {
                        continue;
                    }

                    kept.Add(node?.DeepClone());
                }

                if (kept.Count != all.Count)
                {
                    File.Copy(settingsPath, settingsPath + ".bak", overwrite: true);
                    settings["profiles"]!["list"] = new JsonArray([.. kept]);
                    AtomicFile.WriteText(settingsPath, settings.ToJsonString(AtomicFile.JsonOptions));
                    pruned += all.Count - kept.Count;
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                unreadable.Add(settingsPath);
            }
        }

        return new PruneReport(pruned, unreadable);
    }

    public IReadOnlyList<string> RemoveStaleFragments(IReadOnlyList<string> liveGuids)
    {
        List<string> removed = [];
        string ours = FragmentsFolder();

        if (liveGuids.Count == 0 || !Directory.Exists(ours))
        {
            return removed;
        }

        foreach (string file in Directory.EnumerateFiles(ours, "*.json"))
        {
            JsonObject? fragment = ReadTerminalJson(file);
            JsonArray? profiles = fragment?["profiles"] as JsonArray;
            string target = profiles is { Count: > 0 } && profiles[0] is JsonObject profile
                ? StringOrNull(profile["updates"]) ?? string.Empty
                : string.Empty;

            if (target.Length > 0 && !liveGuids.Contains(target))
            {
                File.Delete(file);
                removed.Add(Path.GetFileName(file));
            }
        }

        return removed;
    }

    public void TouchSettings()
    {
        foreach (string path in _settingsPaths)
        {
            if (File.Exists(path))
            {
                File.SetLastWriteTime(path, DateTime.Now);
            }
        }
    }

    public WslTheme GetAppearance(string instanceName)
    {
        WslTheme theme = WslTheme.Default();
        JsonObject? fragment = ReadTerminalJson(GetFragmentPath(instanceName));
        JsonObject? parsed = (fragment?["profiles"] as JsonArray) is { Count: > 0 } profiles
            ? profiles[0] as JsonObject
            : null;

        if (parsed is not null)
        {
            theme.FontName = StringOrNull(parsed["font"]?["face"]) ?? theme.FontName;
            theme.ColorScheme = StringOrNull(parsed["colorScheme"]) ?? theme.ColorScheme;
        }

        foreach (string settingsPath in _settingsPaths)
        {
            JsonObject? settings = ReadTerminalJson(settingsPath);
            JsonArray? list = settings?["profiles"]?["list"] as JsonArray;

            if (list is null)
            {
                continue;
            }

            foreach (JsonNode? node in list)
            {
                if (node is JsonObject profile &&
                    StringOrNull(profile["name"]) == instanceName &&
                    StringOrNull(profile["source"]) == "Microsoft.WSL")
                {
                    theme.FontName = StringOrNull(profile["font"]?["face"]) ?? theme.FontName;
                    theme.ColorScheme = StringOrNull(profile["colorScheme"]) ?? theme.ColorScheme;
                }
            }
        }

        return theme;
    }

    public IReadOnlyList<WslColorSchemeDto> GetColorSchemes()
    {
        Dictionary<string, JsonObject?> schemes = new(StringComparer.Ordinal);

        foreach (string defaultsPath in TerminalDefaultsPaths())
        {
            AddSchemes(defaultsPath, schemes, overwrite: false);
        }

        foreach (string settingsPath in _settingsPaths)
        {
            AddSchemes(settingsPath, schemes, overwrite: true);
        }

        string ours = FragmentsFolder();

        if (Directory.Exists(ours))
        {
            foreach (string file in Directory.EnumerateFiles(ours, "*.json"))
            {
                JsonObject? fragment = ReadTerminalJson(file);
                string? named = (fragment?["profiles"] as JsonArray) is { Count: > 0 } profiles
                    ? StringOrNull((profiles[0] as JsonObject)?["colorScheme"])
                    : null;

                if (!string.IsNullOrEmpty(named) && !schemes.ContainsKey(named))
                {
                    schemes[named] = null;
                }
            }
        }

        return
        [
            .. schemes.Keys.Order(StringComparer.Ordinal).Select(name => ToSchemeDto(name, schemes[name])),
        ];
    }

    private static void AddSchemes(
        string path,
        Dictionary<string, JsonObject?> schemes,
        bool overwrite)
    {
        JsonObject? settings = ReadTerminalJson(path);

        if (settings?["schemes"] is not JsonArray rows)
        {
            return;
        }

        foreach (JsonNode? node in rows)
        {
            if (node is not JsonObject scheme ||
                StringOrNull(scheme["name"]) is not { Length: > 0 } name)
            {
                continue;
            }

            if (overwrite || !schemes.ContainsKey(name))
            {
                schemes[name] = scheme;
            }
        }
    }

    private static IReadOnlyList<string> TerminalDefaultsPaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        HashSet<string> found = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in new[]
                 {
                     Path.Combine(
                         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Packages",
                         "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
                         "LocalState",
                         "defaults.json"),
                     Path.Combine(
                         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Packages",
                         "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe",
                         "LocalState",
                         "defaults.json"),
                     Path.Combine(
                         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Microsoft",
                         "Windows Terminal",
                         "defaults.json"),
                 })
        {
            if (File.Exists(path))
            {
                found.Add(path);
            }
        }

        foreach (string install in TerminalInstallLocations())
        {
            string defaults = Path.Combine(install, "defaults.json");

            if (File.Exists(defaults))
            {
                found.Add(defaults);
            }
        }

        if (found.Count == 0)
        {
            foreach (string install in PowerShellInstallLocations())
            {
                string defaults = Path.Combine(install, "defaults.json");

                if (File.Exists(defaults))
                {
                    found.Add(defaults);
                }
            }
        }

        return [.. found];
    }

    private static IReadOnlyList<string> TerminalInstallLocations()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        HashSet<string> locations = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            using RegistryKey? repository = Registry.CurrentUser.OpenSubKey(
                @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");

            if (repository is not null)
            {
                foreach (string packageName in repository.GetSubKeyNames())
                {
                    if (!packageName.StartsWith("Microsoft.WindowsTerminal_", StringComparison.OrdinalIgnoreCase) &&
                        !packageName.StartsWith("Microsoft.WindowsTerminalPreview_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using RegistryKey? package = repository.OpenSubKey(packageName);

                    if (package?.GetValue("PackageRootFolder") is string root && Directory.Exists(root))
                    {
                        locations.Add(root);
                    }
                }
            }
        }
        catch (Exception exception) when (
            exception is SecurityException or UnauthorizedAccessException or IOException)
        {
            Trace.TraceWarning($"[TerminalRepository] Registry lookup for Terminal packages failed: {exception.Message}");
        }

        if (locations.Count == 0)
        {
            try
            {
                string apps = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "WindowsApps");

                foreach (string directory in Directory.EnumerateDirectories(apps, "Microsoft.WindowsTerminal*"))
                {
                    locations.Add(directory);
                }
            }
            catch (Exception exception) when (
                exception is SecurityException or UnauthorizedAccessException or IOException)
            {
                Trace.TraceWarning($"[TerminalRepository] WindowsApps enumeration failed: {exception.Message}");
            }
        }

        return [.. locations];
    }

    private static IReadOnlyList<string> PowerShellInstallLocations()
    {
        string powershell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");

        if (!File.Exists(powershell))
        {
            return [];
        }

        try
        {
            ProcessStartInfo start = new()
            {
                FileName = powershell,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-Command");
            start.ArgumentList.Add(
                "Get-AppxPackage | Where-Object { $_.Name -in @('Microsoft.WindowsTerminal', " +
                "'Microsoft.WindowsTerminalPreview') } | ForEach-Object { $_.InstallLocation }");

            using Process? process = Process.Start(start);

            if (process is null)
            {
                return [];
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return
            [
                .. output.Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(Directory.Exists),
            ];
        }
        catch (Exception exception) when (
            exception is Win32Exception or IOException or InvalidOperationException)
        {
            return [];
        }
    }

    public bool IsFontInstalled(string faceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");

        return key?.GetValue($"{faceName} (TrueType)") is not null;
    }

    public void InstallFont(string faceName, string? from)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new WslSquadException(WslSquadErrorCode.PlatformUnsupported, null, "Installing fonts is only supported on Windows.");
        }

        string userFontsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows",
            "Fonts");
        string destination = Path.Combine(userFontsDir, $"{faceName} Regular.ttf");

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
        Directory.CreateDirectory(userFontsDir);
        string temporary = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ttf");

        try
        {
            string source = !string.IsNullOrEmpty(from) && File.Exists(from)
                ? from
                : FindBundledFont(faceName);

            if (source.Length == 0)
            {
                using HttpClient client = new();
                byte[] bytes = client.GetByteArrayAsync(
                    "https://github.com/romkatv/powerlevel10k-media/raw/master/MesloLGS%20NF%20Regular.ttf")
                    .GetAwaiter().GetResult();
                File.WriteAllBytes(temporary, bytes);
                source = temporary;
            }

            if (!File.Exists(destination))
            {
                File.Copy(source, destination, overwrite: true);
            }

            key.SetValue($"{faceName} (TrueType)", destination);
        }
        finally
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[TerminalRepository] Could not delete temporary font '{temporary}': {ex.Message}");
            }
        }
    }

    public IReadOnlyList<TerminalFont> GetTerminalFonts()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            return [];
        }

        string[] styles =
        [
            "Regular", "Bold", "Italic", "Oblique", "Light", "SemiLight", "Medium",
            "Thin", "Black", "Heavy", "SemiBold", "Semibold", "DemiBold", "ExtraBold",
            "ExtraLight", "UltraLight", "Book", "Condensed", "Narrow",
        ];

        HashSet<string> symbols = new(StringComparer.OrdinalIgnoreCase)
        {
            "Wingdings", "Wingdings 2", "Wingdings 3", "Webdings", "Symbol", "Marlett",
        };

        List<string> families;

        try
        {
            families = InstalledFamilyNames();
        }
        catch (Exception exception) when (exception is ExternalException or InvalidOperationException)
        {
            return [];
        }

        families.Sort(StringComparer.Ordinal);
        HashSet<string> known = new(families, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> files = FontFiles();
        List<TerminalFont> fonts = [];

        foreach (string family in families)
        {
            string baseName = family;

            foreach (string style in styles)
            {
                if (family.EndsWith($" {style}", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = family[..^(style.Length + 1)];
                    break;
                }
            }

            if (!string.Equals(baseName, family, StringComparison.Ordinal) &&
                known.Contains(baseName))
            {
                continue;
            }

            if (symbols.Contains(family))
            {
                continue;
            }

            if (IsTerminalFont(family, files))
            {
                fonts.Add(new TerminalFont(family, Icons: true));
            }
        }

        return fonts;
    }

    [SupportedOSPlatform("windows6.1")]
    private static List<string> InstalledFamilyNames()
    {
        using InstalledFontCollection installed = new();

        return [.. installed.Families.Select(family => family.Name)];
    }

    [SupportedOSPlatform("windows6.1")]
    private static bool IsTerminalFont(string family, IReadOnlyDictionary<string, string> files)
    {
        try
        {
            using Font font = new(family, 20);
            using Bitmap bitmap = new(64, 64);
            using Graphics canvas = Graphics.FromImage(bitmap);
            using StringFormat format = StringFormat.GenericTypographic;
            float narrow = canvas.MeasureString("iiii", font, 1000, format).Width;
            float wide = canvas.MeasureString("WWWW", font, 1000, format).Width;

            if (Math.Abs(narrow - wide) >= 0.05f)
            {
                return false;
            }

            string? path = FontFileFor(family, files);

            return path is not null &&
                   FontGlyphReader.HasGlyph(path, 'A') &&
                   FontGlyphReader.HasIconGlyph(path);
        }
        catch (Exception exception) when (exception is ExternalException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows6.1")]
    private static Dictionary<string, string> FontFiles()
    {
        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        foreach (RegistryKey root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            using RegistryKey? key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");

            if (key is null)
            {
                continue;
            }

            foreach (string property in key.GetValueNames())
            {
                string face = property
                    .Replace(" (TrueType)", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace(" (OpenType)", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (face.Length == 0 || files.ContainsKey(face))
                {
                    continue;
                }

                if (key.GetValue(property) is not string value || value.Length == 0)
                {
                    continue;
                }

                files[face] = Path.IsPathRooted(value)
                    ? value
                    : Path.Combine(windows, "Fonts", value);
            }
        }

        return files;
    }

    private static string? FontFileFor(string family, IReadOnlyDictionary<string, string> files)
    {
        if (files.TryGetValue(family, out string? exact))
        {
            return exact;
        }

        foreach ((string face, string path) in files)
        {
            if (face.StartsWith(family + " ", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    public IconDrawing DrawIcon(string instanceName, string outputPath, IconDrawing? drawing)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new WslSquadException(WslSquadErrorCode.PlatformUnsupported, null, "Icon drawing needs Windows.");
        }

        return IconPainter.Draw(instanceName, outputPath, drawing);
    }

    public IReadOnlyList<WslIconPairDto> GetIconPairs()
    {
        return
        [
            .. _palette.Select(pair => new WslIconPairDto
            {
                Name = pair.Name,
                Top = pair.Top,
                Bottom = pair.Bottom,
                TextColor = pair.TextColor,
            }),
        ];
    }

    private string ReadProfileGuid(string instanceName)
    {
        return GetWslGuids(instanceName).ProfileGuid ?? string.Empty;
    }

    private static JsonObject? ReadTerminalJson(string path)
    {
        if (path.Length == 0 || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return ParseObject(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static JsonObject ParseObject(string text)
    {
        return JsonNode.Parse(
            text,
            documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            })?.AsObject()
            ?? throw new WslSquadException(WslSquadErrorCode.CorruptState, null, "The file does not contain a JSON object.");
    }

    private static string? StringOrNull(JsonNode? node)
    {
        try
        {
            return node?.GetValue<string>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string FragmentsFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows Terminal",
            "Fragments",
            "wsl-squad");
    }

    private static WslColorSchemeDto ToSchemeDto(string name, JsonObject? scheme)
    {
        return new WslColorSchemeDto
        {
            Name = name,
            Background = StringOrNull(scheme?["background"]) ?? string.Empty,
            Foreground = StringOrNull(scheme?["foreground"]) ?? string.Empty,
            Black = Colour(scheme, "black", 0),
            DarkRed = Colour(scheme, "red", 1),
            DarkGreen = Colour(scheme, "green", 2),
            DarkYellow = Colour(scheme, "yellow", 3),
            DarkBlue = Colour(scheme, "blue", 4),
            DarkMagenta = Colour(scheme, "purple", 5),
            DarkCyan = Colour(scheme, "cyan", 6),
            Gray = Colour(scheme, "white", 7),
            DarkGray = Colour(scheme, "brightBlack", 8),
            Red = Colour(scheme, "brightRed", 9),
            Green = Colour(scheme, "brightGreen", 10),
            Yellow = Colour(scheme, "brightYellow", 11),
            Blue = Colour(scheme, "brightBlue", 12),
            Magenta = Colour(scheme, "brightPurple", 13),
            Cyan = Colour(scheme, "brightCyan", 14),
            White = Colour(scheme, "brightWhite", 15),
        };
    }

    private static string Colour(JsonObject? scheme, string key, int index)
    {
        string? named = StringOrNull(scheme?[key]);

        if (!string.IsNullOrEmpty(named))
        {
            return named;
        }

        if (scheme?["palette"] is JsonArray palette && palette.Count >= 16)
        {
            return StringOrNull(palette[index]) ?? string.Empty;
        }

        return string.Empty;
    }

    private string FindBundledFont(string faceName)
    {
        string root = Path.Combine(_assetsDir, "fonts");

        if (!Directory.Exists(root))
        {
            return string.Empty;
        }

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .FirstOrDefault(file =>
                (Path.GetExtension(file).Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetExtension(file).Equals(".otf", StringComparison.OrdinalIgnoreCase)) &&
                Path.GetFileName(file).StartsWith(faceName, StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }











    private sealed record IconPair(string Name, string Top, string Bottom, string TextColor);
}
