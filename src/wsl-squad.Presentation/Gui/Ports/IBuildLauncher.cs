using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Gui.Ports;

public interface IBuildLauncher
{
    Task<int> RunAsync(BuildDto request);
}
