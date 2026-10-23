using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public abstract class WorkflowBase
{
    protected WorkflowBase(
        IInstanceService service,
        Action<string>? executionStarted)
    {
        Service = service;
        ExecutionStarted = executionStarted;
    }

    protected IInstanceService Service { get; }

    protected Action<string>? ExecutionStarted { get; }

    protected async Task<(IReadOnlyList<WslIndexDto> Rows, WorkflowResult<T>? Error)> LoadRowsAsync<T>()
    {
        try
        {
            IReadOnlyList<WslIndexDto> rows = await Task.Run(() => Service.GetAll());

            return (rows, null);
        }
        catch (InvalidOperationException exception)
        {
            return ([], WorkflowResults.Failed<T>(string.Empty, exception.Message));
        }
    }

    protected static WorkflowResult<T>? SelectError<T>(
        TargetResolution choice,
        string noTargetMessage)
    {
        return choice.Status switch
        {
            TargetResolutionStatus.None => WorkflowResults.Failed<T>(string.Empty, noTargetMessage),
            TargetResolutionStatus.Cancelled => WorkflowResults.Cancelled<T>(string.Empty),
            _ => null,
        };
    }
}
