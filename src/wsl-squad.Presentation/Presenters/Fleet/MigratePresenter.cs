using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Fleet;

public sealed class MigratePresenter
{
    private readonly FleetWorkflow _workflow;
    private readonly IFleetView _view;

    public MigratePresenter(IFleetService service, IFleetView view)
    {
        _view = view;
        _workflow = new FleetWorkflow(service, new ViewFleetInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<MigrateResponseDto> result = _workflow.MigrateRootAsync(target)
            .GetAwaiter()
            .GetResult();

        if (result.Status == WorkflowStatus.Cancelled)
        {
            _view.ShowCancelled("moved");
        }

        return result.ToActionResult("Migrate", _view.Show, _view.ShowError);
    }
}
