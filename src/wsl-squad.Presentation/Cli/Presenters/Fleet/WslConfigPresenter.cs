using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Fleet;

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
