namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Runs pack operations inside a WSL distribution.</summary>
public interface IWslPackGateway
{
    public Task<IReadOnlyList<string>> GetInstalledPackNamesAsync(
        string instanceName,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<string>> GetDistributionFamilyTokensAsync(
        string instanceName,
        CancellationToken cancellationToken = default);

    public Task<string> GetUserHomeAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default);

    public Task CopyPackIntoDistributionAsync(
        string instanceName,
        string user,
        string windowsSourceDirectory,
        string packName,
        CancellationToken cancellationToken = default);

    public Task OpenPackSudoAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default);

    public Task ClosePackSudoAsync(
        string instanceName,
        CancellationToken cancellationToken = default);

    public Task RunScriptAsync(
        string instanceName,
        string user,
        string? homeDirectory,
        string linuxScriptPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);

    public Task RemovePackFolderAsync(
        string instanceName,
        string linuxHome,
        string packName,
        CancellationToken cancellationToken = default);

    public Task RunCleanupOrphansAsync(
        string instanceName,
        string linuxScriptPath,
        CancellationToken cancellationToken = default);
}
