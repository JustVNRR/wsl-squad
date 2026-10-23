using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestorePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly ArchiveSelector _selector;

    public RestorePresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Restore : {exception.Message}");
        }

        if (selection.Status == TargetSelectionStatus.None)
        {
            return ActionResult.Nok("Restore : there are no archives");
        }

        if (selection.Status == TargetSelectionStatus.Cancelled)
        {
            View.ShowCancelled("created");

            return ActionResult.Ok("Restore : cancelled");
        }

        string archiveName = selection.Name;

        View.ShowRestoring(archiveName);
        string? name = View.AskNewName();

        if (string.IsNullOrEmpty(name))
        {
            View.ShowCancelled("created");

            return ActionResult.Ok("Restore : cancelled");
        }

        try
        {
            RestoreResponseDto response = _service.Restore(new RestoreDto
            {
                ArchiveName = archiveName,
                InstanceName = name,
            });
            View.Show(response);

            return ActionResult.Ok($"Restore : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Restore : {name} - {exception.Message}");
        }
    }
}
