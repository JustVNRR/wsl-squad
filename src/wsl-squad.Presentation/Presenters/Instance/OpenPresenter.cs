using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class OpenPresenter
{
    private readonly InstanceWorkflow _workflow;
    private readonly IInstanceOpenView _view;

    public OpenPresenter(IInstanceService service, IInstanceOpenView view)
    {
        _view = view;
        _workflow = new InstanceWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<OpenResponseDto> result = _workflow.OpenAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Open", _view.Show, _view.ShowError);
    }
}
