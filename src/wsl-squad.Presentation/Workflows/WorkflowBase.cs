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
            return ([], WorkflowResults.Failed<T>(string.Empty, exception.ToMessage()));
        }
    }

    protected async Task<(string? Name, WorkflowResult<T>? Error)> PickTargetAsync<T>(
        string? target,
        bool archives,
        Func<IReadOnlyList<WslIndexDto>, string?> pick,
        string noTargetMessage)
    {
        if (!string.IsNullOrEmpty(target))
        {
            return (target, null);
        }

        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<T>? error) = await LoadRowsAsync<T>();

        if (error is not null)
        {
            return (null, error);
        }

        return PickTarget<T>(target, rows, archives, pick, noTargetMessage);
    }

    protected static (string? Name, WorkflowResult<T>? Error) PickTarget<T>(
        string? target,
        IReadOnlyList<WslIndexDto> rows,
        bool archives,
        Func<IReadOnlyList<WslIndexDto>, string?> pick,
        string noTargetMessage)
    {
        if (!string.IsNullOrEmpty(target))
        {
            if (rows.Count > 0 &&
                !rows.Any(row => row.IsArchive == archives && row.Name == target))
            {
                return (null, WorkflowResults.Failed<T>(
                    target,
                    archives ? "not an archive of this machine" : "not an instance of this machine"));
            }

            return (target, null);
        }

        List<WslIndexDto> candidates = [.. rows.Where(row => row.IsArchive == archives)];

        if (candidates.Count == 0)
        {
            return (null, WorkflowResults.NoTarget<T>(noTargetMessage));
        }

        string? picked = pick(candidates);

        return string.IsNullOrEmpty(picked)
            ? (null, WorkflowResults.Cancelled<T>(string.Empty))
            : (picked, null);
    }
}
