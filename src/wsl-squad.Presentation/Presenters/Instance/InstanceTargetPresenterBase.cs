using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public abstract class InstanceTargetPresenterBase : BasePresenter<IInstanceView>
{
    private readonly TargetSelector _selector;

    protected InstanceTargetPresenterBase(IInstanceService service, IInstanceView view)
        : base(view)
    {
        Service = service;
        _selector = new TargetSelector(service, view);
    }

    protected IInstanceService Service { get; }

    protected IReadOnlyList<WslIndexDto> Rows { get; private set; } = [];

    protected abstract string OperationName { get; }

    public ActionResult Run(string? target)
    {
        TargetSelection selection;

        try
        {
            selection = _selector.Select(target, row => !row.IsArchive);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"{OperationName} : {exception.Message}");
        }

        if (selection.Status == TargetSelectionStatus.None)
        {
            return ActionResult.Nok($"{OperationName} : no instance");
        }

        if (selection.Status == TargetSelectionStatus.Cancelled)
        {
            return ActionResult.Ok($"{OperationName} : cancelled");
        }

        Rows = selection.Rows;

        try
        {
            return ExecuteCore(selection.Name);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"{OperationName} : {selection.Name} - {exception.Message}");
        }
    }

    protected abstract ActionResult ExecuteCore(string instanceName);
}
