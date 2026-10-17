namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Queries and manages WSL distributions through <c>wsl.exe</c>.</summary>
public interface IWslGateway
{
    public Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(
        CancellationToken cancellationToken = default);

    public Task ImportDistributionAsync(
        string name,
        string installPath,
        string tarPath,
        CancellationToken cancellationToken = default);

    public Task UnregisterDistributionAsync(string name, CancellationToken cancellationToken = default);

    public Task TerminateDistributionAsync(string name, CancellationToken cancellationToken = default);

    public Task CopyFileIntoDistributionAsync(
        string name,
        string windowsSourcePath,
        string linuxTargetPath,
        CancellationToken cancellationToken = default);

    public Task RunOnboardingAsync(
        string name,
        string linuxScriptPath,
        string? user,
        CancellationToken cancellationToken = default);

    public Task ExportDistributionAsync(
        string name,
        string tarPath,
        string format,
        CancellationToken cancellationToken = default);

    public Task CompactDistributionAsync(string name, CancellationToken cancellationToken = default);

    public Task StartDistributionAsync(string name, CancellationToken cancellationToken = default);
}
