using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestartPresenter
{
    private readonly InstanceLifecycleWorkflow _workflow;
    private readonly IInstanceView _view;

    public RestartPresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<RestartResponseDto> result = _workflow.RestartAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Restart", _view.Show, _view.ShowError);
    }
}
