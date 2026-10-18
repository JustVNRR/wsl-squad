using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ArchivePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public ArchivePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
        _picker = new SelectInstancePresenter(view);
    }

    public ActionResult Run(string? target)
    {
        IReadOnlyList<WslIndexDto> rows;

        try
        {
            rows = _service.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Archive : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Archive : no instance")
                : ActionResult.Ok("Archive : cancelled");
        }

        View.ShowStopWarning(name);

        if (View.Confirm("Continue?", defaultNo: false) != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Archive : cancelled");
        }

        string archiveName = $"{name}_archive";

        if (rows.Any(row => row.IsArchive && row.Name == archiveName))
        {
            View.ShowReplacing(archiveName);
        }

        View.ShowBackingUp(name);

        try
        {
            ArchiveResponseDto response = _service.Archive(new ArchiveDto
            {
                InstanceName = name,
                Format = "tar.gz",
                Permit = true,
            });
            View.Show(response);

            return ActionResult.Ok($"Archive : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Archive : {name} - {exception.Message}");
        }
    }
}
