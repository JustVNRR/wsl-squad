using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class RestartPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceLifecycleWorkflow _workflow;

    public RestartPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<RestartResponseDto> result = _workflow.RestartAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
