using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Fleet;

public sealed class WslConfigPresenter : BasePresenter<IFleetView>
{
    private readonly FleetWorkflow _workflow;

    public WslConfigPresenter(IFleetService service, IFleetView view)
        : base(view)
    {
        _workflow = new FleetWorkflow(service);
    }

    public IWorkflowResult Run()
    {
        WorkflowResult<WslConfigResponseDto> result = _workflow.OpenWslConfig();

        return Finish(result, View.Show);
    }
}
