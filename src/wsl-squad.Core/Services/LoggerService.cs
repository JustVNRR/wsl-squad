using System.Diagnostics;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public sealed class LoggerService : ILoggerService
{
    private readonly string _path;

    public LoggerService(string path)
    {
        _path = path;
        string? directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrEmpty(directory))
        {
            try
            {
                _ = Directory.CreateDirectory(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[LoggerService] Failed to create log directory '{directory}': {ex.Message}");
            }
        }

        try
        {
            if (File.Exists(_path) && new FileInfo(_path).Length > 1024 * 1024)
            {
                File.Move(_path, _path + ".1", overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[LoggerService] Failed to rotate log file '{_path}': {ex.Message}");
        }

        Append($"==== session {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====");
    }

    public void Write(LogEntryDto entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Append($"{entry.When:yyyy-MM-dd HH:mm:ss} [{entry.Level}] {entry.Message}");
    }

    private void Append(string line)
    {
        if (_path.Length > 0)
        {
            try
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[LoggerService] Failed to append to log file '{_path}': {ex.Message}");
            }
        }

        Console.WriteLine(line);
    }
}
