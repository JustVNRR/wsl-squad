using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class OpenPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceLifecycleWorkflow _workflow;

    public OpenPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceLifecycleWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<OpenResponseDto> result = _workflow.OpenAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
