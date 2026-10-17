using System.Diagnostics;

using wsl_squad.Core.Abstractions.Process;

using SystemProcess = System.Diagnostics.Process;

namespace wsl_squad.Core.Infrastructure.Process;

/// <summary>Starts detached processes such as shells and terminal windows.</summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    public void Start(ProcessInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        ProcessStartInfo startInfo = new(invocation.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        if (!string.IsNullOrWhiteSpace(invocation.WorkingDirectory))
        {
            startInfo.WorkingDirectory = invocation.WorkingDirectory;
        }

        foreach (string argument in invocation.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (KeyValuePair<string, string> variable in invocation.Environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        using SystemProcess? process = SystemProcess.Start(startInfo);
    }
}
