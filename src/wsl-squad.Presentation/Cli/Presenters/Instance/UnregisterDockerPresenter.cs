using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class UnregisterDockerPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceIntegrationWorkflow _workflow;

    public UnregisterDockerPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceIntegrationWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<UnregisterDockerResponseDto> result = _workflow.UnregisterDockerAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show);
    }
}
