using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class SetAppearancePresenter : BasePresenter<IAppearanceView>
{
    private readonly InstanceAppearanceWorkflow _workflow;

    public SetAppearancePresenter(IInstanceService service, IInstanceView instanceView, IAppearanceView view)
        : base(view)
    {
        _workflow = new InstanceAppearanceWorkflow(service, new ViewInstanceInteraction(instanceView, view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<SetAppearanceResponseDto> result = _workflow.AppearanceAsync(target)
            .GetAwaiter()
            .GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
