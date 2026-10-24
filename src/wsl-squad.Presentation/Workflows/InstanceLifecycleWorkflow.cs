using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Localization;

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
        (string? name, WorkflowResult<OpenResponseDto>? error) =
            await PickTargetAsync<OpenResponseDto>(
                target,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (error is not null)
        {
            return error;
        }

        string instance = name!;
        ExecutionStarted?.Invoke($"Opening '{instance}'...");

        try
        {
            OpenResponseDto response = await Task.Run(() => Service.Open(instance));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<OpenResponseDto>(instance, exception.ToMessage());
        }
    }

    public async Task<WorkflowResult<RestartResponseDto>> RestartAsync(string? target)
    {
        (string? name, WorkflowResult<RestartResponseDto>? error) =
            await PickTargetAsync<RestartResponseDto>(
                target,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (error is not null)
        {
            return error;
        }

        string instance = name!;

        if (!_interaction.PromptRestart(instance))
        {
            return WorkflowResults.Cancelled<RestartResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Restarting '{instance}'...");

        try
        {
            RestartResponseDto response = await Task.Run(() => Service.Restart(instance));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RestartResponseDto>(instance, exception.ToMessage());
        }
    }

    public async Task<WorkflowResult<bool>> ManagePacksAsync(string? target)
    {
        (string? name, WorkflowResult<bool>? error) =
            await PickTargetAsync<bool>(
                target,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (error is not null)
        {
            return error;
        }

        string instance = name!;
        ExecutionStarted?.Invoke($"Opening the pack manager for '{instance}'...");

        try
        {
            await Task.Run(() => Service.OpenPackManager(instance));

            return WorkflowResults.Success(instance, true);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<bool>(instance, exception.ToMessage());
        }
    }

    public async Task<WorkflowResult<DuplicateResponseDto>> DuplicateAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<DuplicateResponseDto>? error) = await LoadRowsAsync<DuplicateResponseDto>();

        if (error is not null)
        {
            return error;
        }

        (string? name, WorkflowResult<DuplicateResponseDto>? selectionError) =
            PickTarget<DuplicateResponseDto>(
                target,
                rows,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (selectionError is not null)
        {
            return selectionError;
        }

        string instance = name!;
        string[] taken = [.. rows
            .Where(row => !string.IsNullOrEmpty(row.Name))
            .Select(row => row.Name!)];
        DuplicateDto? request = _interaction.PromptDuplicate(instance, taken);

        if (request is null)
        {
            return WorkflowResults.Cancelled<DuplicateResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Duplicating '{instance}'...");

        try
        {
            DuplicateResponseDto response = await Task.Run(() => Service.Duplicate(request));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<DuplicateResponseDto>(instance, exception.ToMessage());
        }
    }

    public async Task<WorkflowResult<RemoveDto>> RemoveAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<RemoveDto>? error) = await LoadRowsAsync<RemoveDto>();

        if (error is not null)
        {
            return error;
        }

        (string? name, WorkflowResult<RemoveDto>? selectionError) =
            PickTarget<RemoveDto>(
                target,
                rows,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (selectionError is not null)
        {
            return selectionError;
        }

        string instance = name!;
        bool replacesArchive = rows.Any(row =>
            row.IsArchive && row.Name == $"{instance}_archive");
        RemoveDto? request = _interaction.PromptRemove(instance, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<RemoveDto>(instance);
        }

        ExecutionStarted?.Invoke($"Removing '{instance}'...");

        try
        {
            await Task.Run(() => Service.Remove(request));

            return WorkflowResults.Success(instance, request);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RemoveDto>(instance, exception.ToMessage());
        }
    }
}
