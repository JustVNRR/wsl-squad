using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

/// <summary>Appends lines to the log.</summary>
public interface ILoggerService
{
    public void Write(LogEntryDto entry);
}
