using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class ArchivePresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceStorageWorkflow _workflow;

    public ArchivePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<ArchiveResponseDto> result = _workflow.ArchiveAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
