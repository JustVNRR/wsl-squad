using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Reads Windows Terminal profiles and writes instance fragments.</summary>
public interface IWindowsTerminalStore
{
    public OperationResult<string?> GetProfileGuid(string instanceName);

    public OperationResult WriteFragment(string instanceName, TerminalFragmentSettings settings);

    public OperationResult DeleteFragment(string instanceName);

    public OperationResult<IReadOnlyList<WslColorScheme>> GetColorSchemes();
}
