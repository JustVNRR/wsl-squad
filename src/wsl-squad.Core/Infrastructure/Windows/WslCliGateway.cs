using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Queries running distributions through <c>wsl.exe</c>.</summary>
public sealed class WslCliGateway : IWslGateway
{
    private readonly IProcessRunner _processRunner;

    public WslCliGateway(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(
        CancellationToken cancellationToken = default)
    {
        ProcessInvocation invocation = WslCommandBuilder.ListRunning();
        ProcessResult result = await _processRunner.RunAsync(invocation, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new ProcessExecutionException(invocation, result);
        }

        return WslTextDecoder.ParseDistributionList(result.StandardOutput);
    }
}
