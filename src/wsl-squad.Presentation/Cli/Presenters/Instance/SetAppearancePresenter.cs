using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class SetAppearancePresenter
{
    private readonly InstanceAppearanceWorkflow _workflow;
    private readonly IAppearanceView _view;

    public SetAppearancePresenter(IInstanceService service, IAppearanceView view)
    {
        _view = view;
        _workflow = new InstanceAppearanceWorkflow(service, new ViewAppearanceInteraction(view));
    }

    public ActionResult Run(string? target)
    {
        WorkflowResult<SetAppearanceResponseDto> result = _workflow.AppearanceAsync(target)
            .GetAwaiter()
            .GetResult();

        return result.ToActionResult("SetAppearance", _view.Show, _view.ShowError);
    }
}
