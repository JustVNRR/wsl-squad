using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class DeleteArchivePresenter
{
    private readonly InstanceWorkflow _workflow;
    private readonly IInstanceDeleteArchiveView _view;

    public DeleteArchivePresenter(IInstanceService service, IInstanceDeleteArchiveView view)
    {
        _view = view;
        _workflow = new InstanceWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<DeleteArchiveResponseDto> result = _workflow.DeleteArchiveAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("DeleteArchive", _view.Show, _view.ShowError, noTargetReason: "no archive");
    }
}
