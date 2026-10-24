using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;

namespace WslSquad.Presentation.Tests.Support;

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
