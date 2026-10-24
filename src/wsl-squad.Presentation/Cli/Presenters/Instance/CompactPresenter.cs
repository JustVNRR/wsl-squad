using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class CompactPresenter : BasePresenter<IInstanceView>
{
    private readonly InstanceStorageWorkflow _workflow;

    public CompactPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _workflow = new InstanceStorageWorkflow(service, new ViewInstanceInteraction(view));
    }

    public IWorkflowResult Run(string? target)
    {
        WorkflowResult<CompactResponseDto> result = _workflow.CompactAsync(target).GetAwaiter().GetResult();

        return Finish(result, View.Show, View.ShowNoInstances);
    }
}
