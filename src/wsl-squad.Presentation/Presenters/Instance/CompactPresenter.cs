using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class CompactPresenter : InstanceTargetPresenterBase
{
    public CompactPresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Compact";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowBeforeQuestions();
        bool? archiveFirst = View.Confirm("Archive it first?", defaultNo: false);

        if (archiveFirst is null)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Compact : cancelled");
        }

        if (archiveFirst == false)
        {
            View.ShowNoArchive();
        }
        else if (Rows.Any(row => row.IsArchive && row.Name == $"{instanceName}_archive"))
        {
            View.ShowReplacingArchive(instanceName);
        }

        View.ShowCompacting(instanceName, archiveFirst.Value);

        CompactResponseDto response = Service.Compact(new CompactDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        });
        View.Show(response);

        return ActionResult.Ok($"Compact : {instanceName}");
    }
}
