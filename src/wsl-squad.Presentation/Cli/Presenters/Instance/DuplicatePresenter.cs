using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

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
