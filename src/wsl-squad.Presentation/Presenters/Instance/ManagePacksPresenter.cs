using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ManagePacksPresenter : InstanceTargetPresenterBase
{
    public ManagePacksPresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "ManagePacks";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        View.ShowManagingPacks(instanceName);
        Service.OpenPackManager(instanceName);

        return ActionResult.Ok($"ManagePacks : {instanceName}");
    }
}
