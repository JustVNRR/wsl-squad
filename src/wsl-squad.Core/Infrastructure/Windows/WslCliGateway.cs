using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Manages WSL distributions through <c>wsl.exe</c>.</summary>
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
        ProcessResult result = await RunAsync(WslCommandBuilder.ListRunning(), cancellationToken).ConfigureAwait(false);

        return WslTextDecoder.ParseDistributionList(result.StandardOutput);
    }

    public Task ImportDistributionAsync(
        string name,
        string installPath,
        string tarPath,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Import(name, installPath, tarPath), cancellationToken);
    }

    public Task UnregisterDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Unregister(name), cancellationToken);
    }

    public Task TerminateDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Terminate(name), cancellationToken);
    }

    public Task CopyFileIntoDistributionAsync(
        string name,
        string windowsSourcePath,
        string linuxTargetPath,
        CancellationToken cancellationToken = default)
    {
        string wslSourcePath = PathHelper.ToWslMountPath(windowsSourcePath)
            ?? throw new ArgumentException(
                $"'{windowsSourcePath}' cannot be translated to a WSL mount path.",
                nameof(windowsSourcePath));

        return RunAsync(WslCommandBuilder.CopyFile(name, wslSourcePath, linuxTargetPath), cancellationToken);
    }

    public Task RunOnboardingAsync(
        string name,
        string linuxScriptPath,
        string? user,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.RunOnboarding(name, linuxScriptPath, user), cancellationToken);
    }

    private async Task<ProcessResult> RunAsync(ProcessInvocation invocation, CancellationToken cancellationToken)
    {
        ProcessResult result = await _processRunner.RunAsync(invocation, cancellationToken).ConfigureAwait(false);

        return result.Succeeded ? result : throw new ProcessExecutionException(invocation, result);
    }
}
