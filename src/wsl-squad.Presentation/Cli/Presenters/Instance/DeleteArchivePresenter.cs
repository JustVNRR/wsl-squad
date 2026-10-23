using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class DeleteArchivePresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceStorageWorkflow _workflow;

    public DeleteArchivePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<DeleteArchiveResponseDto> result = _workflow.DeleteArchiveAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show);
    }
}
