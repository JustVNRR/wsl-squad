using WslSquad.Core.Dtos;
using WslSquad.Core.Services;

namespace WslSquad.Presentation.Tests.Support;

public sealed class RecordingLogger : ILoggerService
{
    public List<LogEntryDto> Entries { get; } = [];

    public void Write(LogEntryDto entry)
    {
        Entries.Add(entry);
    }
}
