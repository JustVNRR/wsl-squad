using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Opens an instance (reference <c>OpenPresenter.psm1</c>).</summary>
public sealed class OpenPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public OpenPresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Open : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Open : no instance")
                : ActionResult.Ok("Open : cancelled");
        }

        View.ShowOpening(name);

        try
        {
            OpenResponseDto response = _service.Open(name);
            View.Show(response);

            return ActionResult.Ok($"Open : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Open : {name} - {exception.Message}");
        }
    }
}
