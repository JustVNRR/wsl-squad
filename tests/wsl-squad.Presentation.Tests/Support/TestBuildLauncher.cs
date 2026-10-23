using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Gui.Ports;

namespace wsl_squad.Presentation.Tests.Support;

public sealed class TestBuildLauncher : IBuildLauncher
{
    public BuildDto? Request { get; private set; }

    public int ExitCode { get; set; }

    public Task<int> RunAsync(BuildDto request)
    {
        Request = request;

        return Task.FromResult(ExitCode);
    }
}
