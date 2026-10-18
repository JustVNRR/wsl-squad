using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface ILoggerService
{
    public void Write(LogEntryDto entry);
}
