using System.ComponentModel;
using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;

namespace wsl_squad.Core.Application.Lifecycle;

/// <summary>Validates that an instance exists and reports its runtime state.</summary>
public sealed class InstanceStateService
{
    private readonly IFileSystem _fileSystem;
    private readonly FleetMarkerStore _markers;
    private readonly IWslGateway _wsl;

    public InstanceStateService(IFileSystem fileSystem, FleetMarkerStore markers, IWslGateway wsl)
    {
        _fileSystem = fileSystem;
        _markers = markers;
        _wsl = wsl;
    }

    public OperationResult<string> GetManagedPath(string fleetRoot, string name)
    {
        if (!InstanceName.IsValid(name))
        {
            return OperationResult<string>.Fail(ErrorCode.InvalidName, $"Instance name '{name}' is invalid.");
        }

        string path = _fileSystem.Path.Combine(fleetRoot, name);

        if (!_fileSystem.Directory.Exists(path))
        {
            return OperationResult<string>.Fail(ErrorCode.NotFound, $"Instance '{name}' does not exist.");
        }

        if (!_markers.Exists(path))
        {
            return OperationResult<string>.Fail(
                ErrorCode.NotFound,
                $"Folder '{path}' is not managed by wsl-squad.");
        }

        return OperationResult<string>.Ok(path);
    }

    public async Task<OperationResult<bool>> IsRunningAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<string> running = await _wsl
                .GetRunningDistributionNamesAsync(cancellationToken)
                .ConfigureAwait(false);

            return OperationResult<bool>.Ok(running.Contains(name, StringComparer.OrdinalIgnoreCase));
        }
        catch (ProcessExecutionException exception)
        {
            return OperationResult<bool>.Fail(
                ErrorCode.ExternalTool,
                "wsl.exe could not list running distributions.",
                exception.Result.StandardError,
                exception.Result.ExitCode);
        }
        catch (Win32Exception exception)
        {
            return OperationResult<bool>.Fail(
                ErrorCode.ExternalTool,
                "wsl.exe is not available.",
                exception.Message);
        }
    }

    public long GetVirtualDiskSize(string installPath)
    {
        return GetFileSize(_fileSystem.Path.Combine(installPath, "ext4.vhdx"));
    }

    public long GetFileSize(string path)
    {
        try
        {
            return _fileSystem.File.Exists(path) ? _fileSystem.FileInfo.New(path).Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
