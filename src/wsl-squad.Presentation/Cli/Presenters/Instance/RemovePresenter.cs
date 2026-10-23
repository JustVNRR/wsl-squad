using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class RemovePresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceLifecycleWorkflow _workflow;

    public RemovePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<RemoveDto> result = _workflow.RemoveAsync(target).GetAwaiter().GetResult();

        return Finish(result, _ => View.Show(), View.ShowNoInstances);
    }
}
