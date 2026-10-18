using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Compacts an instance (reference <c>CompactPresenter.psm1</c>).</summary>
public sealed class CompactPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public CompactPresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Compact : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Compact : no instance")
                : ActionResult.Ok("Compact : cancelled");
        }

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
        else if (rows.Any(row => row.IsArchive && row.Name == $"{name}_archive"))
        {
            View.ShowReplacingArchive(name);
        }

        View.ShowCompacting(name, archiveFirst.Value);

        try
        {
            CompactResponseDto response = _service.Compact(new CompactDto
            {
                InstanceName = name,
                ArchiveFirst = archiveFirst.Value,
            });
            View.Show(response);

            return ActionResult.Ok($"Compact : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Compact : {name} - {exception.Message}");
        }
    }
}
