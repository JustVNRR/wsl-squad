using wsl_squad.Core.Abstractions.Windows;

namespace wsl_squad.Tests.Support;

public sealed class FakeWslGateway : IWslGateway
{
    private readonly List<string> _runningNames = [];
    private readonly List<string> _importedNames = [];
    private readonly List<string> _unregisteredNames = [];
    private readonly List<string> _terminatedNames = [];
    private readonly List<string> _copiedFiles = [];
    private readonly List<string> _onboardedNames = [];

    public Exception? Failure { get; set; }

    public Exception? ImportFailure { get; set; }

    public Exception? OnboardingFailure { get; set; }

    public IReadOnlyList<string> ImportedNames => _importedNames;

    public IReadOnlyList<string> UnregisteredNames => _unregisteredNames;

    public IReadOnlyList<string> TerminatedNames => _terminatedNames;

    public IReadOnlyList<string> CopiedFiles => _copiedFiles;

    public IReadOnlyList<string> OnboardedNames => _onboardedNames;

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

    public Task ImportDistributionAsync(
        string name,
        string installPath,
        string tarPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _importedNames.Add(name);

        if (ImportFailure is not null)
        {
            throw ImportFailure;
        }

        return Task.CompletedTask;
    }

    public Task UnregisterDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _unregisteredNames.Add(name);

        return Task.CompletedTask;
    }

    public Task TerminateDistributionAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _terminatedNames.Add(name);

        return Task.CompletedTask;
    }

    public Task CopyFileIntoDistributionAsync(
        string name,
        string windowsSourcePath,
        string linuxTargetPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _copiedFiles.Add($"{name}:{windowsSourcePath}->{linuxTargetPath}");

        return Task.CompletedTask;
    }

    public Task RunOnboardingAsync(
        string name,
        string linuxScriptPath,
        string? user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _onboardedNames.Add(name);

        if (OnboardingFailure is not null)
        {
            throw OnboardingFailure;
        }

        return Task.CompletedTask;
    }
}
