using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface IFleetService
{
    string GetRoot();

    string SetRoot(string target);

    IReadOnlyList<string> GetFleet();

    WslConfigResponseDto WslConfig();

    MigrateResponseDto Migrate(string target);
}
