using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

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
        WorkflowResult<bool> result = _workflow.RegisterDockerAsync(target).GetAwaiter().GetResult();

        return Finish(result);
    }
}
