using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Deletes an archive (reference <c>DeleteArchivePresenter.psm1</c>).</summary>
public sealed class DeleteArchivePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;

    public DeleteArchivePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            View.ShowNoName();

            return ActionResult.Nok("DeleteArchive : no name");
        }

        View.ShowDeleting(target);

        try
        {
            DeleteArchiveResponseDto response = _service.DeleteArchive(target);
            View.Show(response);

            return ActionResult.Ok($"DeleteArchive : {target}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"DeleteArchive : {target} - {exception.Message}");
        }
    }
}
