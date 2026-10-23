using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceStorageWorkflow : WorkflowBase
{
    public InstanceStorageWorkflow(IInstanceService service, IInstanceInteraction interaction, Action<string>? executionStarted = null)
        : base(service, interaction, executionStarted)
    {
    }

    public async Task<WorkflowResult<ArchiveResponseDto>> ArchiveAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<ArchiveResponseDto>? error) = await LoadRowsAsync<ArchiveResponseDto>();

        if (error is not null)
        {
            return error;
        }

        TargetResolution choice = Interaction.SelectInstance(target, rows);

        if (SelectError<ArchiveResponseDto>(choice) is { } selectionError)
        {
            return selectionError;
        }

        bool replacesArchive = rows.Any(row =>
            row.IsArchive && row.Name == $"{choice.Name}_archive");
        ArchiveDto? request = Interaction.PromptArchive(choice.Name, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<ArchiveResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Archiving '{choice.Name}'...");

        try
        {
            ArchiveResponseDto response = await Task.Run(() => Service.Archive(request));

            return WorkflowResults.Success<ArchiveResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<ArchiveResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<CompactResponseDto>> CompactAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<CompactResponseDto>? error) = await LoadRowsAsync<CompactResponseDto>();

        if (error is not null)
        {
            return error;
        }

        TargetResolution choice = Interaction.SelectInstance(target, rows);

        if (SelectError<CompactResponseDto>(choice) is { } selectionError)
        {
            return selectionError;
        }

        bool replacesArchive = rows.Any(row =>
            row.IsArchive && row.Name == $"{choice.Name}_archive");
        CompactDto? request = Interaction.PromptCompact(choice.Name, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<CompactResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Compacting '{choice.Name}'...");

        try
        {
            CompactResponseDto response = await Task.Run(() => Service.Compact(request));

            return WorkflowResults.Success<CompactResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<CompactResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<RestoreResponseDto>> RestoreAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<RestoreResponseDto>? error) = await LoadRowsAsync<RestoreResponseDto>();

        if (error is not null)
        {
            return error;
        }

        TargetResolution choice = Interaction.SelectArchive(target, rows);

        if (SelectError<RestoreResponseDto>(choice) is { } selectionError)
        {
            return selectionError;
        }

        string[] taken = [.. rows
            .Where(row => !row.IsArchive && !string.IsNullOrEmpty(row.Name))
            .Select(row => row.Name!)];
        RestoreDto? request = Interaction.PromptRestore(choice.Name, taken);

        if (request is null)
        {
            return WorkflowResults.Cancelled<RestoreResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Restoring '{choice.Name}'...");

        try
        {
            RestoreResponseDto response = await Task.Run(() => Service.Restore(request));

            return WorkflowResults.Success<RestoreResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RestoreResponseDto>(choice.Name, exception.Message);
        }
    }

    public async Task<WorkflowResult<DeleteArchiveResponseDto>> DeleteArchiveAsync(string? target)
    {
        TargetResolution choice;

        if (!string.IsNullOrEmpty(target))
        {
            choice = new TargetResolution(TargetResolutionStatus.Selected, target);
        }
        else
        {
            (IReadOnlyList<WslIndexDto> rows, WorkflowResult<DeleteArchiveResponseDto>? error) =
                await LoadRowsAsync<DeleteArchiveResponseDto>();

            if (error is not null)
            {
                return error;
            }

            choice = Interaction.SelectArchive(target, rows);

            if (SelectError<DeleteArchiveResponseDto>(choice) is { } selectionError)
            {
                return selectionError;
            }
        }

        if (!Interaction.PromptDeleteArchive(choice.Name))
        {
            return WorkflowResults.Cancelled<DeleteArchiveResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Deleting the archive '{choice.Name}'...");

        try
        {
            DeleteArchiveResponseDto response = await Task.Run(() => Service.DeleteArchive(choice.Name));

            return WorkflowResults.Success<DeleteArchiveResponseDto>(choice.Name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<DeleteArchiveResponseDto>(choice.Name, exception.Message);
        }
    }
}
