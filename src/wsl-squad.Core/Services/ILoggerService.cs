using WslSquad.Core.Dtos;

namespace WslSquad.Core.Services;

public interface ILoggerService
{
    void Write(LogEntryDto entry);
}
