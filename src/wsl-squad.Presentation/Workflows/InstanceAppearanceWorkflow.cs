using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Localization;

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
        (string? name, WorkflowResult<SetAppearanceResponseDto>? error) =
            await PickTargetAsync<SetAppearanceResponseDto>(
                target,
                archives: false,
                _interaction.PickInstance,
                "No instance is available.");

        if (error is not null)
        {
            return error;
        }

        string instance = name!;
        AppearanceContextDto context;

        try
        {
            context = await Task.Run(() => Service.GetAppearanceContext(instance));
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<SetAppearanceResponseDto>(instance, exception.ToMessage());
        }

        IReadOnlyList<SetAppearanceDto> requests = _interaction.PromptAppearance(instance, context);

        if (requests.Count == 0)
        {
            return WorkflowResults.Cancelled<SetAppearanceResponseDto>(instance);
        }

        ExecutionStarted?.Invoke($"Applying '{instance}'s appearance...");

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
                InstanceName = instance,
                Changes = changes,
            };

            return WorkflowResults.Success(instance, result);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<SetAppearanceResponseDto>(instance, exception.ToMessage());
        }
    }
}
