namespace wsl_squad.Core.Abstractions.Build;

/// <summary>Builds images and exports root filesystems through the Docker CLI.</summary>
public interface IDockerGateway
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    public Task BuildImageAsync(
        string imageTag,
        string dockerfilePath,
        string contextPath,
        CancellationToken cancellationToken = default);

    public Task<string> LoadImageAsync(string tarPath, CancellationToken cancellationToken = default);

    public Task ExportRootfsAsync(
        string imageReference,
        string outputTarPath,
        CancellationToken cancellationToken = default);

    public Task SaveImageAsync(
        string imageTag,
        string outputTarPath,
        CancellationToken cancellationToken = default);

    public Task RemoveImageAsync(string imageTag, CancellationToken cancellationToken = default);
}
