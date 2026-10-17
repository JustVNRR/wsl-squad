using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Tests.Support;

public sealed class FakeWslGateway : IWslGateway
{
    private readonly List<string> _runningNames = [];

    public Exception? Failure { get; set; }

    public void SetRunning(params string[] names)
    {
        _runningNames.Clear();
        _runningNames.AddRange(names);
    }

    public Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult<IReadOnlyList<string>>(_runningNames);
    }
}
