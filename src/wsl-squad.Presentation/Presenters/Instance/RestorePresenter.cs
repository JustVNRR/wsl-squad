using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestorePresenter
{
    private readonly InstanceStorageWorkflow _workflow;
    private readonly IInstanceView _view;

    public RestorePresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<RestoreResponseDto> result = _workflow.RestoreAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Restore", _view.Show, _view.ShowError, noTargetReason: "no archive");
    }
}
