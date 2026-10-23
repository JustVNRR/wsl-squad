using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceIntegrationWorkflow : WorkflowBase
{
    public InstanceIntegrationWorkflow(IInstanceService service, IInstanceInteraction interaction, Action<string>? executionStarted = null)
        : base(service, interaction, executionStarted)
    {
    }

    public async Task<WorkflowResult<bool>> RegisterDockerAsync(string? target)
    {
        return await DockerAsync(target, register: true);
    }

    public async Task<WorkflowResult<bool>> UnregisterDockerAsync(string? target)
    {
        return await DockerAsync(target, register: false);
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

            choice = Interaction.SelectInstance(target, rows);

            if (SelectError<SetAppearanceResponseDto>(choice) is { } selectionError)
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

        IReadOnlyList<SetAppearanceDto> requests = Interaction.PromptAppearance(choice.Name, context);

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

    private async Task<WorkflowResult<bool>> DockerAsync(string? target, bool register)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<bool>? error) =
            await LoadRowsAsync<bool>();

        if (error is not null)
        {
            return error;
        }

        List<WslIndexDto> live = [.. rows.Where(row => !row.IsArchive)];
        string name;

        if (!string.IsNullOrEmpty(target))
        {
            WslIndexDto? row = live.Find(item => item.Name == target);

            if (row is null)
            {
                return WorkflowResults.Failed<bool>(target, "not an instance of this machine");
            }

            DockerTargetStatus status = DockerRules.Status(row.Docker, register);

            if (status != DockerTargetStatus.Eligible)
            {
                Interaction.ShowDockerProblem(target, Map(status));

                string reason = status switch
                {
                    DockerTargetStatus.AlreadyRegistered => "already registered",
                    DockerTargetStatus.NotRegistered => "not registered",
                    _ => "the Docker Desktop state cannot be read",
                };

                return WorkflowResults.Failed<bool>(target, reason);
            }

            name = target;
        }
        else
        {
            List<WslIndexDto> candidates = [.. live.Where(row => DockerRules.IsCandidate(row, register))];

            if (candidates.Count == 0)
            {
                DockerProblem problem = live.Count > 0 &&
                                        live.TrueForAll(row => DockerRules.Status(row.Docker, register) == DockerTargetStatus.Unknown)
                    ? DockerProblem.Unknown
                    : DockerProblem.NoCandidates;
                Interaction.ShowDockerProblem(string.Empty, problem);

                return WorkflowResults.NoTarget<bool>();
            }

            TargetResolution choice = Interaction.SelectInstance(string.Empty, candidates);

            if (SelectError<bool>(choice) is { } selectionError)
            {
                return selectionError;
            }

            name = choice.Name;
        }

        bool? restart = Interaction.PromptDocker(name, register);

        if (restart is null)
        {
            return WorkflowResults.Cancelled<bool>(name);
        }

        ExecutionStarted?.Invoke(
            register
                ? $"Registering '{name}' with Docker Desktop..."
                : $"Taking '{name}' out of Docker Desktop...");

        try
        {
            if (register)
            {
                _ = await Task.Run(() => Service.RegisterDocker(new RegisterDockerDto
                {
                    InstanceName = name,
                    Restart = restart.Value,
                }));

                return WorkflowResults.Success<bool>(name, true);
            }

            _ = await Task.Run(() => Service.UnregisterDocker(new UnregisterDockerDto
            {
                InstanceName = name,
                Restart = restart.Value,
            }));

            return WorkflowResults.Success<bool>(name, true);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<bool>(name, exception.Message);
        }
    }

    private static DockerProblem Map(DockerTargetStatus status)
    {
        return status switch
        {
            DockerTargetStatus.AlreadyRegistered => DockerProblem.AlreadyRegistered,
            DockerTargetStatus.NotRegistered => DockerProblem.NotRegistered,
            _ => DockerProblem.Unknown,
        };
    }
}
