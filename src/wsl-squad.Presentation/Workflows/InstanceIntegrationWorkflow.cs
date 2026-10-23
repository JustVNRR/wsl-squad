using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceIntegrationWorkflow : WorkflowBase
{
    private readonly IInstanceInteraction _interaction;

    public InstanceIntegrationWorkflow(IInstanceService service, IInstanceInteraction interaction, Action<string>? executionStarted = null)
        : base(service, executionStarted)
    {
        _interaction = interaction;
    }

    public async Task<WorkflowResult<bool>> RegisterDockerAsync(string? target)
    {
        return await DockerAsync(target, register: true);
    }

    public async Task<WorkflowResult<bool>> UnregisterDockerAsync(string? target)
    {
        return await DockerAsync(target, register: false);
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
                _interaction.ShowDockerProblem(target, Map(status));

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
                _interaction.ShowDockerProblem(string.Empty, problem);

                return WorkflowResults.Failed<bool>(
                    string.Empty,
                    "No Docker Desktop candidate is available.");
            }

            TargetResolution choice = _interaction.SelectInstance(string.Empty, candidates);

            if (SelectError<bool>(choice, "No instance is available.") is { } selectionError)
            {
                return selectionError;
            }

            name = choice.Name;
        }

        bool? restart = _interaction.PromptDocker(name, register);

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
