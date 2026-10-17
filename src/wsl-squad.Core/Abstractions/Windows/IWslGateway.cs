namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Queries WSL through its command-line interface.</summary>
public interface IWslGateway
{
    public Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(
        CancellationToken cancellationToken = default);
}
