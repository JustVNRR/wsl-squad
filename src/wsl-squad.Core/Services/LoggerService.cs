using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

/// <summary>Appends the session's lines to the log and the console (reference <c>LoggerService</c>).</summary>
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
            catch (IOException)
            {
                // The reference ignores an unwritable log directory.
            }
        }

        try
        {
            if (File.Exists(_path) && new FileInfo(_path).Length > 1024 * 1024)
            {
                File.Move(_path, _path + ".1", overwrite: true);
            }
        }
        catch (IOException)
        {
            // The reference ignores rotation failures.
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
            catch (IOException)
            {
                // The console line still goes out.
            }
        }

        Console.WriteLine(line);
    }
}
