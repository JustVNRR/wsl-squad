using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Tests.Support;

public sealed class FakeDockerDesktopStore : IDockerDesktopStore
{
    private readonly List<string> _distros = [];

    public bool SettingsAvailable { get; set; } = true;

    public bool ReadFails { get; set; }

    public Exception? WriteFailure { get; set; }

    public IReadOnlyList<string> IntegratedDistros => _distros;

    public int WriteCount { get; private set; }

    public void SetIntegrated(params string[] names)
    {
        _distros.Clear();
        _distros.AddRange(names);
    }

    public OperationResult<DockerDesktopSettings> Read()
    {
        if (ReadFails)
        {
            return OperationResult<DockerDesktopSettings>.Fail(
                ErrorCode.CorruptState,
                "settings unreadable");
        }

        return OperationResult<DockerDesktopSettings>.Ok(
            new DockerDesktopSettings(SettingsAvailable, [.. _distros]));
    }

    public OperationResult SetIntegration(string instanceName, bool integrated)
    {
        WriteCount++;

        if (WriteFailure is not null)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                "settings not writable",
                WriteFailure.Message);
        }

        if (integrated)
        {
            if (!_distros.Contains(instanceName, StringComparer.OrdinalIgnoreCase))
            {
                _distros.Add(instanceName);
            }
        }
        else
        {
            _ = _distros.RemoveAll(name =>
                string.Equals(name, instanceName, StringComparison.OrdinalIgnoreCase));
        }

        return OperationResult.Ok();
    }
}
