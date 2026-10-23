using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class SetAppearancePresenter : BasePresenter<IAppearanceView>
{
    private readonly InstanceAppearanceWorkflow _workflow;

    public SetAppearancePresenter(IInstanceService service, IAppearanceView view)
        : base(view)
    {
        _workflow = new InstanceAppearanceWorkflow(service, new ViewAppearanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<SetAppearanceResponseDto> result = _workflow.AppearanceAsync(target)
            .GetAwaiter()
            .GetResult();

        return Finish(result, View.Show);
    }
}
