using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestartPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public RestartPresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"Restart : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("Restart : no instance")
                : ActionResult.Ok("Restart : cancelled");
        }

        View.ShowStoppingWarning(name);

        if (View.AskRestart() != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Restart : cancelled");
        }

        View.ShowRestarting(name);

        try
        {
            RestartResponseDto response = _service.Restart(name);
            View.Show(response);

            return ActionResult.Ok($"Restart : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Restart : {name} - {exception.Message}");
        }
    }
}
