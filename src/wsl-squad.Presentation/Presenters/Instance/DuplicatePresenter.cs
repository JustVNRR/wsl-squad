using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Duplicates an instance (reference <c>DuplicatePresenter.psm1</c>).</summary>
public sealed class DuplicatePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public DuplicatePresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Duplicate : {exception.Message}");
        }

        string source = _picker.Pick(target, rows);

        if (source.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Duplicate : no instance")
                : ActionResult.Ok("Duplicate : cancelled");
        }

        string copy;

        while (true)
        {
            string? answer = View.AskCopyName();

            if (string.IsNullOrEmpty(answer))
            {
                View.ShowCancelled("created");

                return ActionResult.Ok("Duplicate : cancelled");
            }

            if (NameRules.IsValid(answer))
            {
                copy = answer;

                break;
            }

            View.ShowNameHint();
        }

        View.ShowDiskWarning(source);

        if (View.Confirm("Continue?", defaultNo: false) != true)
        {
            View.ShowCancelled("created");

            return ActionResult.Ok("Duplicate : cancelled");
        }

        View.ShowDuplicating(source, copy);

        try
        {
            DuplicateResponseDto response = _service.Duplicate(new DuplicateDto
            {
                SourceName = source,
                CopyName = copy,
            });
            View.Show(response);

            return ActionResult.Ok($"Duplicate : {copy}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Duplicate : {source} - {exception.Message}");
        }
    }
}
