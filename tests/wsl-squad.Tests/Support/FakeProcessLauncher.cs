using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Tests.Support;

public sealed class FakeProcessLauncher : IProcessLauncher
{
    private readonly List<ProcessInvocation> _invocations = [];

    public IReadOnlyList<ProcessInvocation> Invocations => _invocations;

    public Exception? Failure { get; set; }

    public void Start(ProcessInvocation invocation)
    {
        _invocations.Add(invocation);

        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
