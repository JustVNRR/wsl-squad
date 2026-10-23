using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class OpenPresenter
{
    private readonly InstanceLifecycleWorkflow _workflow;
    private readonly IInstanceView _view;

    public OpenPresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<OpenResponseDto> result = _workflow.OpenAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Open", _view.Show, _view.ShowError);
    }
}
