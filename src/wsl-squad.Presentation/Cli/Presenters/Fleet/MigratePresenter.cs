using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Fleet;

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
