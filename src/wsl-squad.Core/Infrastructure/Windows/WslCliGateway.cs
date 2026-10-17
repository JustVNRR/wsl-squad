using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Manages WSL distributions through <c>wsl.exe</c>.</summary>
public sealed class WslCliGateway : IWslGateway, IWslPackGateway
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

    public Task ExportDistributionAsync(
        string name,
        string tarPath,
        string format,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Export(name, tarPath, format), cancellationToken);
    }

    public Task CompactDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Compact(name), cancellationToken);
    }

    public Task StartDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        return RunAsync(WslCommandBuilder.Start(name), cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetInstalledPackNamesAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        ProcessResult result = await RunAsync(PackCommandBuilder.InstalledPacks(instanceName), cancellationToken)
            .ConfigureAwait(false);

        return WslPackOutputParser.ParseInstalledPackNames(result.StandardOutput);
    }

    public async Task<IReadOnlyList<string>> GetDistributionFamilyTokensAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        ProcessResult result = await RunAsync(PackCommandBuilder.OsRelease(instanceName), cancellationToken)
            .ConfigureAwait(false);

        return WslPackOutputParser.ParseFamilyTokens(result.StandardOutput);
    }

    public async Task<string> GetUserHomeAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default)
    {
        ProcessResult result = await RunAsync(PackCommandBuilder.UserHome(instanceName, user), cancellationToken)
            .ConfigureAwait(false);

        return result.StandardOutput.Trim();
    }

    public Task CopyPackIntoDistributionAsync(
        string instanceName,
        string user,
        string windowsSourceDirectory,
        string packName,
        CancellationToken cancellationToken = default)
    {
        string wslSourceDirectory = PathHelper.ToWslMountPath(windowsSourceDirectory)
            ?? throw new ArgumentException(
                $"'{windowsSourceDirectory}' cannot be translated to a WSL mount path.",
                nameof(windowsSourceDirectory));

        return RunAsync(
            PackCommandBuilder.CopyPack(instanceName, user, wslSourceDirectory, packName),
            cancellationToken);
    }

    public Task OpenPackSudoAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(PackCommandBuilder.OpenSudo(instanceName, user), cancellationToken);
    }

    public Task ClosePackSudoAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        return RunAsync(PackCommandBuilder.CloseSudo(instanceName), cancellationToken);
    }

    public Task RunScriptAsync(
        string instanceName,
        string user,
        string? homeDirectory,
        string linuxScriptPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            PackCommandBuilder.RunScript(instanceName, user, homeDirectory, linuxScriptPath, arguments),
            cancellationToken);
    }

    public Task RemovePackFolderAsync(
        string instanceName,
        string linuxHome,
        string packName,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            PackCommandBuilder.RemovePackFolder(instanceName, linuxHome, packName),
            cancellationToken);
    }

    public Task RunCleanupOrphansAsync(
        string instanceName,
        string linuxScriptPath,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            PackCommandBuilder.CleanupOrphans(instanceName, linuxScriptPath),
            cancellationToken);
    }

    private async Task<ProcessResult> RunAsync(ProcessInvocation invocation, CancellationToken cancellationToken)
    {
        ProcessResult result = await _processRunner.RunAsync(invocation, cancellationToken).ConfigureAwait(false);

        return result.Succeeded ? result : throw new ProcessExecutionException(invocation, result);
    }
}
