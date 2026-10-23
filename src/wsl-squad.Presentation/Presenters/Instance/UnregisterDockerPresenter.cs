using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class UnregisterDockerPresenter
{
    private readonly InstanceWorkflow _workflow;
    private readonly IInstanceDockerView _view;

    public UnregisterDockerPresenter(IInstanceService service, IInstanceDockerView view)
    {
        _view = view;
        _workflow = new InstanceWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<bool> result = _workflow.UnregisterDockerAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("UnregisterDocker", null, _view.ShowError);
    }
}
