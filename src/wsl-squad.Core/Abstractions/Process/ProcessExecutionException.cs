namespace wsl_squad.Core.Abstractions.Process;

/// <summary>Thrown when an external tool exits with a non-zero code.</summary>
public sealed class ProcessExecutionException : Exception
{
    public ProcessExecutionException(ProcessInvocation invocation, ProcessResult result)
        : base(BuildMessage(invocation, result))
    {
        Invocation = invocation;
        Result = result;
    }

    public ProcessInvocation Invocation { get; }

    public ProcessResult Result { get; }

    private static string BuildMessage(ProcessInvocation invocation, ProcessResult result)
    {
        string detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();

        return $"{invocation.FileName} exited with code {result.ExitCode}: {detail}";
    }
}
