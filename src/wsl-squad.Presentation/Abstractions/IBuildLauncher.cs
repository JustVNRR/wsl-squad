using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Abstractions;

public interface IBuildLauncher
{
    Task<int> RunAsync(BuildDto request);
}
