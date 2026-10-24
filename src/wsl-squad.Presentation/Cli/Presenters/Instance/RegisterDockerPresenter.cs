using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class RegisterDockerPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceIntegrationWorkflow _workflow;

    public RegisterDockerPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceIntegrationWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<RegisterDockerResponseDto> result = _workflow.RegisterDockerAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show);
    }
}
