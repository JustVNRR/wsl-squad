using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Fleet;

public sealed class MigratePresenter : BasePresenter<IFleetView>
{
    private readonly FleetWorkflow _workflow;

    public MigratePresenter(IFleetService service, IFleetView view)
        : base(view)
    {
        _workflow = new FleetWorkflow(service, new ViewFleetInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<MigrateResponseDto> result = _workflow.MigrateRootAsync(target)
            .GetAwaiter()
            .GetResult();

        if (result.Status == WorkflowStatus.Cancelled)
        {
            View.ShowCancelled("moved");
        }

        return Finish(result, View.Show);
    }
}
