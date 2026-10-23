using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class CompactPresenter
{
    private readonly InstanceStorageWorkflow _workflow;
    private readonly IInstanceView _view;

    public CompactPresenter(IInstanceService service, IInstanceView view)
    {
        _view = view;
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<CompactResponseDto> result = _workflow.CompactAsync(target).GetAwaiter().GetResult();

        return result.ToActionResult("Compact", _view.Show, _view.ShowError);
    }
}
