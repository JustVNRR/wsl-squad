using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceAppearanceWorkflow : WorkflowBase
{
    private readonly IAppearanceInteraction _interaction;

    public InstanceAppearanceWorkflow(
        IInstanceService service,
        IAppearanceInteraction interaction,
        Action<string>? executionStarted = null)
        : base(service, executionStarted)
    {
        _interaction = interaction;
    }

    public async Task<WorkflowResult<SetAppearanceResponseDto>> AppearanceAsync(string? target)
    {
        TargetResolution choice;

        if (!string.IsNullOrEmpty(target))
        {
            choice = new TargetResolution(TargetResolutionStatus.Selected, target);
        }
        else
        {
            (IReadOnlyList<WslIndexDto> rows, WorkflowResult<SetAppearanceResponseDto>? error) =
                await LoadRowsAsync<SetAppearanceResponseDto>();

            if (error is not null)
            {
                return error;
            }

            choice = _interaction.SelectInstance(target, rows);

            if (SelectError<SetAppearanceResponseDto>(choice, "No instance is available.") is { } selectionError)
            {
                return selectionError;
            }
        }

        AppearanceContextDto context;

        try
        {
            context = await Task.Run(() => Service.GetAppearanceContext(choice.Name));
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<SetAppearanceResponseDto>(choice.Name, exception.Message);
        }

        IReadOnlyList<SetAppearanceDto> requests = _interaction.PromptAppearance(choice.Name, context);

        if (requests.Count == 0)
        {
            return WorkflowResults.Cancelled<SetAppearanceResponseDto>(choice.Name);
        }

        ExecutionStarted?.Invoke($"Applying '{choice.Name}'s appearance...");

        try
        {
            List<string> changes = [];

            foreach (SetAppearanceDto request in requests)
            {
                SetAppearanceResponseDto response = await Task.Run(() => Service.SetAppearance(request));
                changes.AddRange(response.Changes);
            }

            SetAppearanceResponseDto result = new()
            {
                InstanceName = choice.Name,
                Changes = changes,
            };

            return WorkflowResults.Success(choice.Name, result);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<SetAppearanceResponseDto>(choice.Name, exception.Message);
        }
    }
}
