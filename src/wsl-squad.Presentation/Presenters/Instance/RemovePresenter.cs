using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RemovePresenter : InstanceTargetPresenterBase
{
    public RemovePresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Remove";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowDestructionGate(
            instanceName,
            $"The WSL distribution '{instanceName}' and ALL its data will be deleted:");
        string? typed = View.AskTypedName();
        View.ShowGateClose();

        if (typed != instanceName)
        {
            View.ShowGateRefused();

            return ActionResult.Ok("Remove : cancelled");
        }

        View.ShowGateOpened();
        bool? archiveFirst = View.Confirm("Archive it before deleting?", defaultNo: true);

        if (archiveFirst is null)
        {
            View.ShowCancelled("deleted");

            return ActionResult.Ok("Remove : cancelled");
        }

        if (archiveFirst == true &&
            Rows.Any(row => row.IsArchive && row.Name == $"{instanceName}_archive"))
        {
            View.ShowReplacingArchive(instanceName);
        }

        View.ShowRemovingSteps();

        Service.Remove(new RemoveDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        });
        View.Show();

        return ActionResult.Ok($"Remove : {instanceName}");
    }
}
