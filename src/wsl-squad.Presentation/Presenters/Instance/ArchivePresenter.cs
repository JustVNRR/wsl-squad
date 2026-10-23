using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ArchivePresenter
{
    private readonly InstanceStorageWorkflow _workflow;
    private readonly IInstanceView _view;

    public ArchivePresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<ArchiveResponseDto> result = _workflow.ArchiveAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Archive", _view.Show, _view.ShowError);
    }
}
