using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class ManagePacksPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceLifecycleWorkflow _workflow;

    public ManagePacksPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<bool> result = _workflow.ManagePacksAsync(target).GetAwaiter().GetResult();

        return Finish(result, _ => View.ShowManagingPacks(result.Subject), View.ShowNoInstances);
    }
}
