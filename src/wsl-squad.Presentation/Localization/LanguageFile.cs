using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace WslSquad.Presentation.Localization;

internal sealed record LanguageFile(
    string? Code,
    string? DisplayName,
    Dictionary<string, string>? Strings)
{
    private const string _embeddedPrefix = "WslSquad.Presentation.languages.";
    private const string _embeddedEnglish = "WslSquad.Presentation.languages.en.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static LanguageFile? Read(string path)
    {
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Trace.TraceWarning($"[Localizer] Could not read language file '{path}': {exception.Message}");

            return null;
        }
    }

    public static LanguageFile ReadEmbeddedEnglish()
    {
        LanguageFile? file = ReadEmbeddedLanguages()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Code, Localizer.English, StringComparison.OrdinalIgnoreCase));

        return file ?? throw new InvalidOperationException(
            $"The embedded language resource '{_embeddedEnglish}' is missing or invalid.");
    }

    public static IReadOnlyList<LanguageFile> ReadEmbeddedLanguages()
    {
        Assembly assembly = typeof(Localizer).Assembly;
        List<LanguageFile> files = [];

        foreach (string resource in assembly.GetManifestResourceNames()
                     .Where(name =>
                         name.StartsWith(_embeddedPrefix, StringComparison.Ordinal) &&
                         Path.GetExtension(name).Equals(".json", StringComparison.OrdinalIgnoreCase)))
        {
            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using StreamReader reader = new(stream);
            LanguageFile? file = Parse(reader.ReadToEnd());

            if (file?.Strings is not null)
            {
                files.Add(file);
            }
        }

        return files;
    }

    private static LanguageFile? Parse(string json)
    {
        LanguageFile? file = JsonSerializer.Deserialize<LanguageFile>(json, _jsonOptions);

        return file is not null && !string.IsNullOrWhiteSpace(file.Code) ? file : null;
    }
}
