using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ManagePacksPresenter
{
    private readonly InstanceWorkflow _workflow;
    private readonly IInstanceManagePacksView _view;

    public ManagePacksPresenter(IInstanceService service, IInstanceManagePacksView view)
    {
        _view = view;
        _workflow = new InstanceWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<bool> result = _workflow.ManagePacksAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult(
            "ManagePacks",
            _ => _view.ShowManagingPacks(result.Subject),
            _view.ShowError);
    }
}
