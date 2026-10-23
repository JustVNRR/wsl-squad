using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public interface IFleetWorkflow
{
    Task<WorkflowResult<MigrateResponseDto>> MigrateRootAsync(string? target);

    WorkflowResult<WslConfigResponseDto> OpenWslConfig();
}

public interface IFleetInteraction
{
    string? AskRoot(string currentRoot);

    void ShowSameFolder();

    void ShowTargetProblem(string problem);

    void ShowLead();
}

public sealed class FleetWorkflow : IFleetWorkflow
{
    private readonly IFleetService _service;
    private readonly IFleetInteraction? _interaction;
    private readonly Action<string>? _executionStarted;

    public FleetWorkflow(
        IFleetService service,
        IFleetInteraction? interaction = null,
        Action<string>? executionStarted = null)
    {
        _service = service;
        _interaction = interaction;
        _executionStarted = executionStarted;
    }

    public async Task<WorkflowResult<MigrateResponseDto>> MigrateRootAsync(string? target)
    {
        string destination;

        if (string.IsNullOrEmpty(target))
        {
            if (_interaction is null)
            {
                return WorkflowResults.Failed<MigrateResponseDto>(
                    string.Empty,
                    "no destination was given.");
            }

            string root;

            try
            {
                root = await Task.Run(() => _service.GetRoot());
            }
            catch (InvalidOperationException exception)
            {
                return WorkflowResults.Failed<MigrateResponseDto>(string.Empty, exception.Message);
            }

            bool chosen = false;
            destination = string.Empty;

            while (!chosen)
            {
                string? answer = _interaction.AskRoot(root);

                if (string.IsNullOrEmpty(answer))
                {
                    return WorkflowResults.Cancelled<MigrateResponseDto>(string.Empty);
                }

                string candidate = Normalize(answer);

                if (string.Equals(candidate, root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    _interaction.ShowSameFolder();

                    continue;
                }

                string? problem = ReferenceHelpers.TestFleetTarget(candidate);

                if (problem is not null)
                {
                    _interaction.ShowTargetProblem(problem);

                    continue;
                }

                destination = candidate;
                chosen = true;
            }
        }
        else
        {
            destination = Normalize(target);
            string? problem = ReferenceHelpers.TestFleetTarget(destination);

            if (problem is not null)
            {
                return WorkflowResults.Failed<MigrateResponseDto>(destination, problem);
            }
        }

        _interaction?.ShowLead();
        _executionStarted?.Invoke($"Moving the fleet to '{destination}'...");

        try
        {
            MigrateResponseDto response = await Task.Run(() => _service.Migrate(destination));

            return WorkflowResults.Success(destination, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<MigrateResponseDto>(destination, exception.Message);
        }
    }

    public WorkflowResult<WslConfigResponseDto> OpenWslConfig()
    {
        try
        {
            WslConfigResponseDto response = _service.WslConfig();

            return WorkflowResults.Success(response.Path ?? string.Empty, response);
        }
        catch (InvalidOperationException exception)
        {
            return WorkflowResults.Failed<WslConfigResponseDto>(string.Empty, exception.Message);
        }
    }

    private static string Normalize(string target)
    {
        return target.Trim().Trim('"').TrimEnd('\\');
    }
}
