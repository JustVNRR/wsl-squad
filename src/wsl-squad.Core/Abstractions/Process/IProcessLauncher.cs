namespace wsl_squad.Core.Abstractions.Process;

/// <summary>Starts a process without waiting for it to exit.</summary>
public interface IProcessLauncher
{
    public void Start(ProcessInvocation invocation);
}
