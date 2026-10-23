using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Fleet;

public sealed class WslConfigPresenter
{
    private readonly FleetWorkflow _workflow;
    private readonly IFleetView _view;

    public WslConfigPresenter(IFleetService service, IFleetView view)
    {
        _view = view;
        _workflow = new FleetWorkflow(service);
    }

    public ActionResult Run()
    {
        _view.ShowLead();

        WorkflowResult<WslConfigResponseDto> result = _workflow.OpenWslConfig();

        return result.ToActionResult("WslConfig", _view.Show, _view.ShowError);
    }
}
