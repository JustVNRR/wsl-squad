namespace wsl_squad.Core.Abstractions.Process;

/// <summary>Executes external processes.</summary>
public interface IProcessRunner
{
    public Task<ProcessResult> RunAsync(ProcessInvocation invocation, CancellationToken cancellationToken = default);
}
