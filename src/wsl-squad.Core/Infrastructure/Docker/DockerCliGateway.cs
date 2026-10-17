using System.ComponentModel;
using System.IO.Abstractions;
using System.Security.Cryptography;

using wsl_squad.Core.Abstractions.Build;
using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Infrastructure.Docker;

/// <summary>Executes Docker operations through the Docker CLI.</summary>
public sealed class DockerCliGateway : IDockerGateway
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;

    public DockerCliGateway(IProcessRunner processRunner, IFileSystem fileSystem)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ProcessResult result = await _processRunner
                .RunAsync(DockerCommandBuilder.Info(), cancellationToken)
                .ConfigureAwait(false);

            return result.Succeeded;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    public Task BuildImageAsync(
        string imageTag,
        string dockerfilePath,
        string contextPath,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            DockerCommandBuilder.Build(imageTag, dockerfilePath, contextPath),
            cancellationToken);
    }

    public async Task<string> LoadImageAsync(string tarPath, CancellationToken cancellationToken = default)
    {
        await RunAsync(DockerCommandBuilder.Load(tarPath), cancellationToken).ConfigureAwait(false);

        using Stream archive = _fileSystem.File.OpenRead(tarPath);
        OperationResult<string> reference = DockerImageArchiveReader.ReadImageReference(archive);

        return reference.IsSuccess
            ? reference.Value
            : throw new InvalidDataException(reference.Error!.Message, null);
    }

    public async Task ExportRootfsAsync(
        string imageReference,
        string outputTarPath,
        CancellationToken cancellationToken = default)
    {
        string containerName = $"wsl-squad-export-{RandomNumberGenerator.GetHexString(8).ToLowerInvariant()}";

        try
        {
            await RunAsync(
                DockerCommandBuilder.CreateContainer(containerName, imageReference),
                cancellationToken).ConfigureAwait(false);

            await RunAsync(
                DockerCommandBuilder.ExportContainer(outputTarPath, containerName),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await RunAsync(
                    DockerCommandBuilder.RemoveContainer(containerName),
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (ProcessExecutionException)
            {
                // The container may not exist when creation failed.
            }
            catch (Win32Exception)
            {
                // Docker may be unavailable; nothing left to clean up.
            }
        }
    }

    public Task SaveImageAsync(
        string imageTag,
        string outputTarPath,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(DockerCommandBuilder.Save(outputTarPath, imageTag), cancellationToken);
    }

    public Task RemoveImageAsync(string imageTag, CancellationToken cancellationToken = default)
    {
        return RunAsync(DockerCommandBuilder.RemoveImage(imageTag), cancellationToken);
    }

    private async Task RunAsync(ProcessInvocation invocation, CancellationToken cancellationToken)
    {
        ProcessResult result = await _processRunner
            .RunAsync(invocation, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new ProcessExecutionException(invocation, result);
        }
    }
}
