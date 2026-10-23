using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceLifecycleWorkflow : WorkflowBase
{
    private readonly IInstanceInteraction _interaction;

    public InstanceLifecycleWorkflow(IInstanceService service, IInstanceInteraction interaction, Action<string>? executionStarted = null)
        : base(service, executionStarted)
    {
        _interaction = interaction;
    }

    public async Task<WorkflowResult<OpenResponseDto>> OpenAsync(string? target)
    {
        TargetResolution choice;

        if (!string.IsNullOrEmpty(target))
        {
            choice = new TargetResolution(TargetResolutionStatus.Selected, target);
        }
        else
        {
            (IReadOnlyList<WslIndexDto> rows, WorkflowResult<OpenResponseDto>? error) = await LoadRowsAsync<OpenResponseDto>();

            if (error is not null)
            {
                return error;
            }

            choice = _interaction.SelectInstance(target, rows);

            if (SelectError<OpenResponseDto>(choice, "No instance is available.") is { } selectionError)
            {
                return selectionError;
            }
        }

        ExecutionStarted?.Invoke($"Opening '{choice.Name}'...");

        try
        {
            OpenResponseDto response = await Task.Run(() => Service.Open(choice.Name));

            return WorkflowResults.Success<OpenResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<OpenResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<RestartResponseDto>> RestartAsync(string? target)
    {
        TargetResolution choice;

        if (!string.IsNullOrEmpty(target))
        {
            choice = new TargetResolution(TargetResolutionStatus.Selected, target);
        }
        else
        {
            (IReadOnlyList<WslIndexDto> rows, WorkflowResult<RestartResponseDto>? error) = await LoadRowsAsync<RestartResponseDto>();

            if (error is not null)
            {
                return error;
            }

            choice = _interaction.SelectInstance(target, rows);

            if (SelectError<RestartResponseDto>(choice, "No instance is available.") is { } selectionError)
            {
                return selectionError;
            }
        }

        if (!_interaction.PromptRestart(choice.Name))
        {
            return WorkflowResults.Cancelled<RestartResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Restarting '{choice.Name}'...");

        try
        {
            RestartResponseDto response = await Task.Run(() => Service.Restart(choice.Name));

            return WorkflowResults.Success<RestartResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RestartResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<bool>> ManagePacksAsync(string? target)
    {
        TargetResolution choice;

        if (!string.IsNullOrEmpty(target))
        {
            choice = new TargetResolution(TargetResolutionStatus.Selected, target);
        }
        else
        {
            (IReadOnlyList<WslIndexDto> rows, WorkflowResult<bool>? error) = await LoadRowsAsync<bool>();

            if (error is not null)
            {
                return error;
            }

            choice = _interaction.SelectInstance(target, rows);

            if (SelectError<bool>(choice, "No instance is available.") is { } selectionError)
            {
                return selectionError;
            }
        }

        ExecutionStarted?.Invoke($"Opening the pack manager for '{choice.Name}'...");

        try
        {
            await Task.Run(() => Service.OpenPackManager(choice.Name));

            return WorkflowResults.Success<bool>(choice.Name, true);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<bool>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<DuplicateResponseDto>> DuplicateAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<DuplicateResponseDto>? error) = await LoadRowsAsync<DuplicateResponseDto>();

        if (error is not null)
        {
            return error;
        }

        TargetResolution choice = _interaction.SelectInstance(target, rows);

        if (SelectError<DuplicateResponseDto>(choice, "No instance is available.") is { } selectionError)
        {
            return selectionError;
        }

        string[] taken = [.. rows
            .Where(row => !string.IsNullOrEmpty(row.Name))
            .Select(row => row.Name!)];
        DuplicateDto? request = _interaction.PromptDuplicate(choice.Name, taken);

        if (request is null)
        {
            return WorkflowResults.Cancelled<DuplicateResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Duplicating '{choice.Name}'...");

        try
        {
            DuplicateResponseDto response = await Task.Run(() => Service.Duplicate(request));

            return WorkflowResults.Success<DuplicateResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<DuplicateResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<RemoveDto>> RemoveAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<RemoveDto>? error) = await LoadRowsAsync<RemoveDto>();

        if (error is not null)
        {
            return error;
        }

        TargetResolution choice = _interaction.SelectInstance(target, rows);

        if (SelectError<RemoveDto>(choice, "No instance is available.") is { } selectionError)
        {
            return selectionError;
        }

        bool replacesArchive = rows.Any(row =>
            row.IsArchive && row.Name == $"{choice.Name}_archive");
        RemoveDto? request = _interaction.PromptRemove(choice.Name, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<RemoveDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Removing '{choice.Name}'...");

        try
        {
            await Task.Run(() => Service.Remove(request));

            return WorkflowResults.Success<RemoveDto>(choice.Name, request);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RemoveDto>(choice.Name, exception.Message);
        }
    }
}
