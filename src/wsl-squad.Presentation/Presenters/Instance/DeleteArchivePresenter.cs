using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class DeleteArchivePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly ArchiveSelector _selector;

    public DeleteArchivePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
        _selector = new ArchiveSelector(service, view);
    }

    public ActionResult Run(string? target)
    {
        TargetSelection selection;

        try
        {
            selection = _selector.Select(target);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"DeleteArchive : {exception.Message}");
        }

        if (selection.Status == TargetSelectionStatus.None)
        {
            return ActionResult.Nok("DeleteArchive : no archive");
        }

        if (selection.Status == TargetSelectionStatus.Cancelled)
        {
            View.ShowCancelled("deleted");

            return ActionResult.Ok("DeleteArchive : cancelled");
        }

        string archiveName = selection.Name;
        bool? confirmed = View.Confirm(
            $"Delete the archive '{archiveName}'? It cannot be restored.",
            defaultNo: true);

        if (confirmed != true)
        {
            View.ShowCancelled("deleted");

            return ActionResult.Ok("DeleteArchive : cancelled");
        }

        View.ShowDeleting(archiveName);

        try
        {
            DeleteArchiveResponseDto response = _service.DeleteArchive(archiveName);
            View.Show(response);

            return ActionResult.Ok($"DeleteArchive : {archiveName}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"DeleteArchive : {archiveName} - {exception.Message}");
        }
    }
}
