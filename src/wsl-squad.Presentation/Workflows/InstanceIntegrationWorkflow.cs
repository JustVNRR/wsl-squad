using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceIntegrationWorkflow : WorkflowBase
{
    private readonly IInstanceInteraction _interaction;

    public InstanceIntegrationWorkflow(
        IInstanceService service,
        IInstanceInteraction interaction,
        Action<string>? executionStarted = null)
        : base(service, executionStarted)
    {
        _interaction = interaction;
    }

    public Task<WorkflowResult<RegisterDockerResponseDto>> RegisterDockerAsync(string? target)
    {
        return RunDockerAsync(
            target,
            register: true,
            name => $"Registering '{name}' with Docker Desktop...",
            (name, restart) => Service.RegisterDocker(new RegisterDockerDto
            {
                InstanceName = name,
                Restart = restart,
            }));
    }

    public Task<WorkflowResult<UnregisterDockerResponseDto>> UnregisterDockerAsync(string? target)
    {
        return RunDockerAsync(
            target,
            register: false,
            name => $"Taking '{name}' out of Docker Desktop...",
            (name, restart) => Service.UnregisterDocker(new UnregisterDockerDto
            {
                InstanceName = name,
                Restart = restart,
            }));
    }

    private async Task<WorkflowResult<TResponse>> RunDockerAsync<TResponse>(
        string? target,
        bool register,
        Func<string, string> progressMessage,
        Func<string, bool, TResponse> execute)
    {
        (IReadOnlyList<WslIndexDto> rows, WorkflowResult<TResponse>? error) =
            await LoadRowsAsync<TResponse>();

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
                return WorkflowResults.Failed<TResponse>(target, "not an instance of this machine");
            }

            DockerTargetStatus status = DockerRules.Status(row.Docker, register);

            if (status != DockerTargetStatus.Eligible)
            {
                string reason = status switch
                {
                    DockerTargetStatus.AlreadyRegistered => "already registered",
                    DockerTargetStatus.NotRegistered => "not registered",
                    _ => "the Docker Desktop state cannot be read",
                };

                return WorkflowResults.Failed<TResponse>(target, reason);
            }

            name = target;
        }
        else
        {
            List<WslIndexDto> candidates = [.. live.Where(row => DockerRules.IsCandidate(row, register))];

            if (candidates.Count == 0)
            {
                string noTargetMessage = live.Count > 0 &&
                                         live.TrueForAll(row =>
                                             DockerRules.Status(row.Docker, register) == DockerTargetStatus.Unknown)
                    ? "the Docker Desktop state cannot be read"
                    : "No Docker Desktop candidate is available.";

                return WorkflowResults.NoTarget<TResponse>(noTargetMessage);
            }

            string? picked = _interaction.PickInstance(candidates);

            if (string.IsNullOrEmpty(picked))
            {
                return WorkflowResults.Cancelled<TResponse>(string.Empty);
            }

            name = picked;
        }

        bool? restart = _interaction.PromptDocker(name, register);

        if (restart is null)
        {
            return WorkflowResults.Cancelled<TResponse>(name);
        }

        ExecutionStarted?.Invoke(progressMessage(name));

        try
        {
            TResponse response = await Task.Run(() => execute(name, restart.Value));

            return WorkflowResults.Success(name, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<TResponse>(name, exception.Message);
        }
    }
}
