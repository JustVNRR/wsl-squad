using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ManagePacksPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public ManagePacksPresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"ManagePacks : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("ManagePacks : no instance")
                : ActionResult.Ok("ManagePacks : cancelled");
        }

        View.ShowManagingPacks(name);

        try
        {
            _service.OpenPackManager(name);

            return ActionResult.Ok($"ManagePacks : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"ManagePacks : {name} - {exception.Message}");
        }
    }
}
