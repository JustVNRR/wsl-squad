using System.ComponentModel;

using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Core.Application.Common;

/// <summary>Runs rollback actions in reverse order and reports every failure.</summary>
public sealed class OperationTransaction
{
    private readonly Stack<Func<Task>> _rollbackActions = new();

    public void OnRollback(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _rollbackActions.Push(action);
    }

    public async Task<IReadOnlyList<string>> RollbackAsync()
    {
        List<string> failures = [];

        while (_rollbackActions.Count > 0)
        {
            Func<Task> action = _rollbackActions.Pop();

            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is ProcessExecutionException
                                                  or IOException
                                                  or UnauthorizedAccessException
                                                  or Win32Exception
                                                  or InvalidOperationException)
            {
                failures.Add(exception.Message);
            }
        }

        return failures;
    }
}
