using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

/// <summary>The fleet root and its overall operations.</summary>
public interface IFleetService
{
    public string GetRoot();

    public string SetRoot(string target);

    public IReadOnlyList<string> GetFleet();

    public WslConfigResponseDto WslConfig();

    public MigrateResponseDto Migrate(string target);
}
