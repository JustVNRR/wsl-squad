using System.Diagnostics;

using wsl_squad.Core.Abstractions.Process;

using SystemProcess = System.Diagnostics.Process;

namespace wsl_squad.Core.Infrastructure.Process;

/// <summary>Executes external processes with redirected output and cancellation support.</summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        ProcessInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new(invocation.FileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
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

        using SystemProcess process = new() { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Process '{invocation.FileName}' could not be started.");
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process has already exited.
            }
        });

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new ProcessResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }
}
