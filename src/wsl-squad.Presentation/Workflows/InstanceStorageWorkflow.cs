using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceStorageWorkflow : WorkflowBase
{
    private readonly IInstanceInteraction _interaction;

    public InstanceStorageWorkflow(IInstanceService service, IInstanceInteraction interaction, Action<string>? executionStarted = null)
        : base(service, executionStarted)
    {
        _interaction = interaction;
    }

    public async Task<WorkflowResult<ArchiveResponseDto>> ArchiveAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<ArchiveResponseDto>? error) = await LoadRowsAsync<ArchiveResponseDto>();

        if (error is not null)
        {
            return error;
        }

        (string? name, WorkflowResult<ArchiveResponseDto>? selectionError) =
            PickTarget<ArchiveResponseDto>(
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
        ArchiveDto? request = _interaction.PromptArchive(instance, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<ArchiveResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Archiving '{instance}'...");

        try
        {
            ArchiveResponseDto response = await Task.Run(() => Service.Archive(request));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<ArchiveResponseDto>(instance, exception.Message);
        }
    }

    public async Task<WorkflowResult<CompactResponseDto>> CompactAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<CompactResponseDto>? error) = await LoadRowsAsync<CompactResponseDto>();

        if (error is not null)
        {
            return error;
        }

        (string? name, WorkflowResult<CompactResponseDto>? selectionError) =
            PickTarget<CompactResponseDto>(
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
        CompactDto? request = _interaction.PromptCompact(instance, replacesArchive);

        if (request is null)
        {
            return WorkflowResults.Cancelled<CompactResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Compacting '{instance}'...");

        try
        {
            CompactResponseDto response = await Task.Run(() => Service.Compact(request));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<CompactResponseDto>(instance, exception.Message);
        }
    }

    public async Task<WorkflowResult<RestoreResponseDto>> RestoreAsync(string? target)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<RestoreResponseDto>? error) = await LoadRowsAsync<RestoreResponseDto>();

        if (error is not null)
        {
            return error;
        }

        (string? name, WorkflowResult<RestoreResponseDto>? selectionError) =
            PickTarget<RestoreResponseDto>(
                target,
                rows,
                archives: true,
                _interaction.PickArchive,
                "No archive is available.");

        if (selectionError is not null)
        {
            return selectionError;
        }

        string instance = name!;
        string[] taken = [.. rows
            .Where(row => !row.IsArchive && !string.IsNullOrEmpty(row.Name))
            .Select(row => row.Name!)];
        RestoreDto? request = _interaction.PromptRestore(instance, taken);

        if (request is null)
        {
            return WorkflowResults.Cancelled<RestoreResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Restoring '{instance}'...");

        try
        {
            RestoreResponseDto response = await Task.Run(() => Service.Restore(request));

            return WorkflowResults.Success(instance, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<RestoreResponseDto>(instance, exception.Message);
        }
    }

    public async Task<WorkflowResult<DeleteArchiveResponseDto>> DeleteArchiveAsync(string? target)
    {
        (string? name, WorkflowResult<DeleteArchiveResponseDto>? error) =
            await PickTargetAsync<DeleteArchiveResponseDto>(
                target,
                archives: true,
                _interaction.PickArchive,
                "No archive is available.");

        if (error is not null)
        {
            return error;
        }

        string archive = name!;

        if (!_interaction.PromptDeleteArchive(archive))
        {
            return WorkflowResults.Cancelled<DeleteArchiveResponseDto>(archive);
        }

        ExecutionStarted?.Invoke($"Deleting the archive '{archive}'...");

        try
        {
            DeleteArchiveResponseDto response = await Task.Run(() => Service.DeleteArchive(archive));

            return WorkflowResults.Success(archive, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<DeleteArchiveResponseDto>(archive, exception.Message);
        }
    }
}
