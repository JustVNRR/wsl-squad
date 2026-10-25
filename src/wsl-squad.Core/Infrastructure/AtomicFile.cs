using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace WslSquad.Core.Infrastructure;

internal static class AtomicFile
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static string? ReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static T? ReadJson<T>(string path)
        where T : class
    {
        string? text = ReadText(path);

        if (text is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void WriteText(string path, string content)
    {
        string directory = Path.GetDirectoryName(path) ?? string.Empty;

        if (directory.Length > 0)
        {
            Directory.CreateDirectory(directory);
        }

        string temporary = path + ".tmp";

        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[AtomicFile] Could not delete '{temporary}': {exception.Message}");
            }

            throw;
        }
    }

    public static void WriteJson<T>(string path, T value)
    {
        WriteText(path, JsonSerializer.Serialize(value, JsonOptions));
    }
}
