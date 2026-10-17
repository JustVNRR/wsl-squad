using wsl_squad.Core.Abstractions.Build;

namespace wsl_squad.Tests.Support;

public sealed class FakeDockerGateway : IDockerGateway
{
    private readonly List<string> _calls = [];

    public bool Available { get; set; } = true;

    public string LoadedImageReference { get; set; } = "loaded:latest";

    public Exception? BuildFailure { get; set; }

    public Exception? ExportFailure { get; set; }

    public Exception? SaveFailure { get; set; }

    public IReadOnlyList<string> Calls => _calls;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Available);
    }

    public Task BuildImageAsync(
        string imageTag,
        string dockerfilePath,
        string contextPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add($"build {imageTag}");

        if (BuildFailure is not null)
        {
            throw BuildFailure;
        }

        return Task.CompletedTask;
    }

    public Task<string> LoadImageAsync(string tarPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add($"load {tarPath}");

        return Task.FromResult(LoadedImageReference);
    }

    public Task ExportRootfsAsync(
        string imageReference,
        string outputTarPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add($"export {imageReference}->{outputTarPath}");

        if (ExportFailure is not null)
        {
            throw ExportFailure;
        }

        return Task.CompletedTask;
    }

    public Task SaveImageAsync(
        string imageTag,
        string outputTarPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add($"save {imageTag}->{outputTarPath}");

        if (SaveFailure is not null)
        {
            throw SaveFailure;
        }

        return Task.CompletedTask;
    }

    public Task RemoveImageAsync(string imageTag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add($"rmi {imageTag}");

        return Task.CompletedTask;
    }
}
