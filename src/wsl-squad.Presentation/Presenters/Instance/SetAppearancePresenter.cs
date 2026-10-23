using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class SetAppearancePresenter
{
    private readonly InstanceWorkflow _workflow;
    private readonly IAppearanceView _view;

    public SetAppearancePresenter(IInstanceService service, IAppearanceView view)
    {
        _view = view;
        _workflow = new InstanceWorkflow(service, new ViewInstanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<SetAppearanceResponseDto> result = _workflow.AppearanceAsync(target)
            .GetAwaiter()
            .GetResult();

        return result.ToActionResult("SetAppearance", _view.Show, _view.ShowError);
    }
}
