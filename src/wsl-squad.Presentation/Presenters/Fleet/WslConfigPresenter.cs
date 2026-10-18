using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Fleet;

public sealed class WslConfigPresenter : BasePresenter<IFleetView>
{
    private readonly IFleetService _service;

    public WslConfigPresenter(IFleetService service, IFleetView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run()
    {
        View.ShowLead();

        try
        {
            WslConfigResponseDto report = _service.WslConfig();
            View.Show(report);

            return ActionResult.Ok($"WslConfig : {report.Path}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"WslConfig : {exception.Message}");
        }
    }
}
