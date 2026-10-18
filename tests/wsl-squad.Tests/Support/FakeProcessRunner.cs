using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Tests.Support;

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<ProcessInvocation> _invocations = [];
    private readonly Queue<ProcessResult> _queuedResults = new();

    public IReadOnlyList<ProcessInvocation> Invocations => _invocations;

    public ProcessResult DefaultResult { get; set; } = new(0, string.Empty, string.Empty);

    public FakeProcessRunner Enqueue(ProcessResult result)
    {
        _queuedResults.Enqueue(result);
        return this;
    }

    public Task<ProcessResult> RunAsync(ProcessInvocation invocation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _invocations.Add(invocation);

        return Task.FromResult(_queuedResults.Count > 0 ? _queuedResults.Dequeue() : DefaultResult);
    }
}
