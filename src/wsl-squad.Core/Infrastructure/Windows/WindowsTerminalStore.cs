using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

using wsl_squad.Core.Abstractions.Platform;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Manages Windows Terminal fragments and reads its colour schemes.</summary>
public sealed class WindowsTerminalStore : IWindowsTerminalStore
{
    private static readonly string[] _colorKeys =
    [
        "black", "red", "green", "yellow", "blue", "purple", "cyan", "white",
        "brightBlack", "brightRed", "brightGreen", "brightYellow",
        "brightBlue", "brightPurple", "brightCyan", "brightWhite",
    ];

    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IFileSystem _fileSystem;
    private readonly string _wslFragmentsRoot;
    private readonly string _ourFragmentsRoot;
    private readonly string[] _settingsPaths;

    public WindowsTerminalStore(IFileSystem fileSystem, IPlatformEnvironment platform)
    {
        _fileSystem = fileSystem;
        string localAppData = platform.LocalApplicationData;
        string fragmentsRoot = fileSystem.Path.Combine(localAppData, "Microsoft", "Windows Terminal", "Fragments");
        _wslFragmentsRoot = fileSystem.Path.Combine(fragmentsRoot, "Microsoft.WSL");
        _ourFragmentsRoot = fileSystem.Path.Combine(fragmentsRoot, "wsl-squad");
        _settingsPaths =
        [
            fileSystem.Path.Combine(
                localAppData,
                "Packages",
                "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
                "LocalState",
                "settings.json"),
            fileSystem.Path.Combine(
                localAppData,
                "Packages",
                "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe",
                "LocalState",
                "settings.json"),
            fileSystem.Path.Combine(localAppData, "Microsoft", "Windows Terminal", "settings.json"),
        ];
    }

    public OperationResult<string?> GetProfileGuid(string instanceName)
    {
        if (!_fileSystem.Directory.Exists(_wslFragmentsRoot))
        {
            return OperationResult<string?>.Ok(null);
        }

        try
        {
            foreach (string file in _fileSystem.Directory.GetFiles(_wslFragmentsRoot, "*.json"))
            {
                string? guid = FindProfileGuid(file, instanceName);

                if (guid is not null)
                {
                    return OperationResult<string?>.Ok(guid);
                }
            }
        }
        catch (IOException exception)
        {
            return OperationResult<string?>.Fail(
                ErrorCode.TerminalUnavailable,
                "Windows Terminal WSL fragments could not be read.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<string?>.Fail(
                ErrorCode.TerminalUnavailable,
                "Windows Terminal WSL fragments could not be read.",
                exception.Message);
        }

        return OperationResult<string?>.Ok(null);
    }

    public OperationResult WriteFragment(string instanceName, TerminalFragmentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        JsonObject profile = new()
        {
            ["updates"] = settings.ProfileGuid,
            ["suppressApplicationTitle"] = true,
        };

        if (!string.IsNullOrWhiteSpace(settings.IconPath))
        {
            profile["icon"] = settings.IconPath;
        }

        if (!string.IsNullOrWhiteSpace(settings.Font))
        {
            profile["font"] = new JsonObject { ["face"] = settings.Font };
        }

        if (!string.IsNullOrWhiteSpace(settings.ColorScheme))
        {
            profile["colorScheme"] = settings.ColorScheme;
        }

        if (!string.IsNullOrWhiteSpace(settings.TabTitle))
        {
            profile["tabTitle"] = settings.TabTitle;
        }

        JsonObject fragment = new()
        {
            ["profiles"] = new JsonArray(profile),
        };

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(_ourFragmentsRoot);
            AtomicFileWriter.WriteAllText(
                _fileSystem,
                GetFragmentPath(instanceName),
                fragment.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                "The Windows Terminal fragment could not be written.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                "The Windows Terminal fragment could not be written.",
                exception.Message);
        }
    }

    public OperationResult DeleteFragment(string instanceName)
    {
        string path = GetFragmentPath(instanceName);

        try
        {
            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Delete(path);
            }

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                "The Windows Terminal fragment could not be deleted.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                "The Windows Terminal fragment could not be deleted.",
                exception.Message);
        }
    }

    public OperationResult<IReadOnlyList<WslColorScheme>> GetColorSchemes()
    {
        Dictionary<string, WslColorScheme> schemes = new(StringComparer.OrdinalIgnoreCase);

        foreach (string settingsPath in _settingsPaths)
        {
            if (!_fileSystem.File.Exists(settingsPath))
            {
                continue;
            }

            try
            {
                string json = JsoncSanitizer.Sanitize(_fileSystem.File.ReadAllText(settingsPath));
                using JsonDocument document = JsonDocument.Parse(json, _documentOptions);

                if (!document.RootElement.TryGetProperty("schemes", out JsonElement schemesElement) ||
                    schemesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement schemeElement in schemesElement.EnumerateArray())
                {
                    WslColorScheme? scheme = ReadScheme(schemeElement);

                    if (scheme is not null)
                    {
                        schemes[scheme.Name] = scheme;
                    }
                }
            }
            catch (JsonException)
            {
                // A damaged Windows Terminal settings file should not block appearance.
            }
            catch (IOException)
            {
                // Same for an unreadable file.
            }
        }

        return OperationResult<IReadOnlyList<WslColorScheme>>.Ok(
            [.. schemes.Values.OrderBy(scheme => scheme.Name, StringComparer.OrdinalIgnoreCase)]);
    }

    private string GetFragmentPath(string instanceName)
    {
        return _fileSystem.Path.Combine(_ourFragmentsRoot, $"{instanceName}.json");
    }

    private string? FindProfileGuid(string fragmentPath, string instanceName)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(
                _fileSystem.File.ReadAllText(fragmentPath),
                _documentOptions);

            if (!document.RootElement.TryGetProperty("profiles", out JsonElement profiles) ||
                profiles.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (JsonElement profile in profiles.EnumerateArray())
            {
                if (!profile.TryGetProperty("name", out JsonElement nameElement) ||
                    nameElement.GetString() is not string name ||
                    !string.Equals(name, instanceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (profile.TryGetProperty("guid", out JsonElement guidElement) &&
                    guidElement.GetString() is string guid)
                {
                    return guid;
                }
            }
        }
        catch (JsonException)
        {
            // Damaged fragments are ignored.
        }
        catch (IOException)
        {
            // Unreadable fragments are ignored.
        }

        return null;
    }

    private static WslColorScheme? ReadScheme(JsonElement element)
    {
        if (!element.TryGetProperty("name", out JsonElement nameElement) ||
            nameElement.GetString() is not string name ||
            string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        string background = element.TryGetProperty("background", out JsonElement backgroundElement)
            ? backgroundElement.GetString() ?? "#000000"
            : "#000000";
        string foreground = element.TryGetProperty("foreground", out JsonElement foregroundElement)
            ? foregroundElement.GetString() ?? "#FFFFFF"
            : "#FFFFFF";
        string[] palette = ReadPalette(element);
        List<string> colors = [];

        for (int index = 0; index < _colorKeys.Length; index++)
        {
            string key = _colorKeys[index];
            string color = "#000000";

            if (element.TryGetProperty(key, out JsonElement colorElement) &&
                colorElement.GetString() is string namedColor)
            {
                color = namedColor;
            }
            else if (index < palette.Length)
            {
                color = palette[index];
            }

            colors.Add(color);
        }

        return new WslColorScheme(name, background, foreground, colors);
    }

    private static string[] ReadPalette(JsonElement element)
    {
        if (!element.TryGetProperty("palette", out JsonElement paletteElement) ||
            paletteElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<string> colors = [];

        foreach (JsonElement color in paletteElement.EnumerateArray())
        {
            if (color.GetString() is string value)
            {
                colors.Add(value);
            }
        }

        return [.. colors];
    }
}
