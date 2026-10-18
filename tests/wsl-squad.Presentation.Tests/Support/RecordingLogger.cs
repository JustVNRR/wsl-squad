using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Tests.Support;

/// <summary>Captures the log lines written by a ViewModel.</summary>
public sealed class RecordingLogger : ILoggerService
{
    public List<LogEntryDto> Entries { get; } = [];

    public void Write(LogEntryDto entry)
    {
        Entries.Add(entry);
    }
}
