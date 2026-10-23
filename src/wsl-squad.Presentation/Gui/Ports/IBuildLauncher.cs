using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Gui.Ports;

public interface IBuildLauncher
{
    Task<int> RunAsync(BuildDto request);
}
