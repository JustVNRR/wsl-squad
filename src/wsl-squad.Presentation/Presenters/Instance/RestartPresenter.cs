using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestartPresenter : InstanceTargetPresenterBase
{
    public RestartPresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Restart";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowStoppingWarning(instanceName);

        if (View.AskRestart() != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Restart : cancelled");
        }

        View.ShowRestarting(instanceName);

        RestartResponseDto response = Service.Restart(instanceName);
        View.Show(response);

        return ActionResult.Ok($"Restart : {instanceName}");
    }
}
