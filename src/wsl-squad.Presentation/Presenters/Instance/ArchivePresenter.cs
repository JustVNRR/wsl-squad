using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ArchivePresenter : InstanceTargetPresenterBase
{
    public ArchivePresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Archive";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowStopWarning(instanceName);

        if (View.Confirm("Continue?", defaultNo: false) != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Archive : cancelled");
        }

        string archiveName = $"{instanceName}_archive";

        if (Rows.Any(row => row.IsArchive && row.Name == archiveName))
        {
            View.ShowReplacing(archiveName);
        }

        View.ShowBackingUp(instanceName);

        ArchiveResponseDto response = Service.Archive(new ArchiveDto
        {
            InstanceName = instanceName,
            Format = "tar.gz",
            Permit = true,
        });
        View.Show(response);

        return ActionResult.Ok($"Archive : {instanceName}");
    }
}
