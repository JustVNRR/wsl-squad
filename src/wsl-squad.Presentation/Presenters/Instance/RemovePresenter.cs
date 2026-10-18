using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RemovePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public RemovePresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Remove : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Remove : no instance")
                : ActionResult.Ok("Remove : cancelled");
        }

        View.ShowDestructionGate(
            name,
            $"The WSL distribution '{name}' and ALL its data will be deleted:");
        string? typed = View.AskTypedName();
        View.ShowGateClose();

        if (typed != name)
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
            rows.Any(row => row.IsArchive && row.Name == $"{name}_archive"))
        {
            View.ShowReplacingArchive(name);
        }

        View.ShowRemovingSteps();

        try
        {
            _service.Remove(new RemoveDto
            {
                InstanceName = name,
                ArchiveFirst = archiveFirst.Value,
            });
            View.Show();

            return ActionResult.Ok($"Remove : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Remove : {name} - {exception.Message}");
        }
    }
}
