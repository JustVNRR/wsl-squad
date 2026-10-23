using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

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
        WorkflowResult<bool> result = _workflow.UnregisterDockerAsync(target).GetAwaiter().GetResult();

        return Finish(result);
    }
}
