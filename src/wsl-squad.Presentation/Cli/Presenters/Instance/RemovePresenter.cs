using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class RemovePresenter
{
    private readonly InstanceLifecycleWorkflow _workflow;
    private readonly IInstanceView _view;

    public RemovePresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<RemoveDto> result = _workflow.RemoveAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Remove", _ => _view.Show(), _view.ShowError);
    }
}
