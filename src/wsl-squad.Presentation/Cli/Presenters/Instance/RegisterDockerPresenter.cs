using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class RegisterDockerPresenter
{
    private readonly InstanceIntegrationWorkflow _workflow;
    private readonly IInstanceView _view;

    public RegisterDockerPresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceIntegrationWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<bool> result = _workflow.RegisterDockerAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("RegisterDocker", null, _view.ShowError);
    }
}
