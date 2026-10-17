using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Docker Desktop settings relevant to WSL integration.</summary>
public sealed record DockerDesktopSettings(bool SettingsAvailable, IReadOnlyList<string> IntegratedDistros);

/// <summary>Reads and updates Docker Desktop's integrated WSL distributions.</summary>
public interface IDockerDesktopStore
{
    public OperationResult<DockerDesktopSettings> Read();

    public OperationResult SetIntegration(string instanceName, bool integrated);
}
