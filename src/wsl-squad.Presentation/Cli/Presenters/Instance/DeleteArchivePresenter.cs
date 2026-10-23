using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class DeleteArchivePresenter
{
    private readonly InstanceStorageWorkflow _workflow;
    private readonly IInstanceView _view;

    public DeleteArchivePresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<DeleteArchiveResponseDto> result = _workflow.DeleteArchiveAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("DeleteArchive", _view.Show, _view.ShowError, noTargetReason: "no archive");
    }
}
