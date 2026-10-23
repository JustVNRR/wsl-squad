using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class OpenPresenter : InstanceTargetPresenterBase
{
    public OpenPresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Open";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowOpening(instanceName);
        OpenResponseDto response = Service.Open(instanceName);
        View.Show(response);

        return ActionResult.Ok($"Open : {instanceName}");
    }
}
