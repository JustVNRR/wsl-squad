using WslSquad.Core.Dtos;

namespace WslSquad.Core.Services;

public interface IFleetService
{
    string GetRoot();

    string SetRoot(string target);

    IReadOnlyList<string> GetFleet();

    WslConfigResponseDto WslConfig();

    MigrateResponseDto Migrate(string target);
}
