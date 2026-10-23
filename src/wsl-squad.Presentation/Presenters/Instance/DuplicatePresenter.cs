using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class DuplicatePresenter
{
    private readonly InstanceLifecycleWorkflow _workflow;
    private readonly IInstanceView _view;

    public DuplicatePresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<DuplicateResponseDto> result = _workflow.DuplicateAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Duplicate", _view.Show, _view.ShowError);
    }
}
