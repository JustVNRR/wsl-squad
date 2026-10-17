using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Tests.Support;

public sealed class FakeWslPackGateway : IWslPackGateway
{
    private readonly List<string> _installed = [];
    private readonly List<string> _family = [];
    private readonly List<string> _calls = [];

    public string Home { get; set; } = "/home/jane";

    public Exception? Failure { get; set; }

    public bool FailOnUserScript { get; set; }

    public IReadOnlyList<string> Calls => _calls;

    public void SetInstalled(params string[] names)
    {
        _installed.Clear();
        _installed.AddRange(names);
    }

    public void SetFamily(params string[] tokens)
    {
        _family.Clear();
        _family.AddRange(tokens);
    }

    public Task<IReadOnlyList<string>> GetInstalledPackNamesAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"installed:{instanceName}");

        return Task.FromResult<IReadOnlyList<string>>(_installed);
    }

    public Task<IReadOnlyList<string>> GetDistributionFamilyTokensAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"family:{instanceName}");

        return Task.FromResult<IReadOnlyList<string>>(_family);
    }

    public Task<string> GetUserHomeAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"home:{instanceName}:{user}");

        return Task.FromResult(Home);
    }

    public Task CopyPackIntoDistributionAsync(
        string instanceName,
        string user,
        string windowsSourceDirectory,
        string packName,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"copy:{user}:{packName}");
        ThrowIfFailed();

        return Task.CompletedTask;
    }

    public Task OpenPackSudoAsync(
        string instanceName,
        string user,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"sudo-open:{user}");
        ThrowIfFailed();

        return Task.CompletedTask;
    }

    public Task ClosePackSudoAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        _calls.Add("sudo-close");

        return Task.CompletedTask;
    }

    public Task RunScriptAsync(
        string instanceName,
        string user,
        string? homeDirectory,
        string linuxScriptPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"run:{user}:{linuxScriptPath}");

        if (FailOnUserScript && !string.Equals(user, "root", StringComparison.Ordinal))
        {
            throw new ProcessExecutionException(
                new ProcessInvocation("wsl.exe", ["-d", instanceName, "--", "/bin/bash", linuxScriptPath]),
                new ProcessResult(1, string.Empty, "script failed"));
        }

        ThrowIfFailed();

        return Task.CompletedTask;
    }

    public Task RemovePackFolderAsync(
        string instanceName,
        string linuxHome,
        string packName,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"rm-folder:{packName}");
        ThrowIfFailed();

        return Task.CompletedTask;
    }

    public Task RunCleanupOrphansAsync(
        string instanceName,
        string linuxScriptPath,
        CancellationToken cancellationToken = default)
    {
        _calls.Add($"cleanup:{linuxScriptPath}");
        ThrowIfFailed();

        return Task.CompletedTask;
    }

    private void ThrowIfFailed()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
