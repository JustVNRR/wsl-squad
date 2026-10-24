using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class DuplicatePresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceLifecycleWorkflow _workflow;

    public DuplicatePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<DuplicateResponseDto> result = _workflow.DuplicateAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
